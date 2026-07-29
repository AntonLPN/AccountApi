using System.Data.Common;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Register;

public class RegisterUserHandler(
    ILogger<RegisterUserHandler> logger,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository,
    ILoginAuditRepository  loginAuditRepository,
    ICryptography cryptographyService,
    IPublishEndpoint publishEndpoint,
    IUserAccountService userAccountService)
    : ICommandHandler<RegisterCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        Email normalizedEmail = Email.Create(request.Email);
        var userByEmail = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail), cancellationToken);
        if (userByEmail is not null)
            return Result<RegisterUserResult>.Conflict("User already exists");

        var keycloakIdUser = await userAccountService.RegisterUserAsync(normalizedEmail, request.Password);
        if (!keycloakIdUser.IsSuccess)
            return Result<RegisterUserResult>.Error(keycloakIdUser.Errors.FirstOrDefault() ?? "Registration failed");
        
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var whoInvited = await userRepository.FirstOrDefaultAsync(new UserByReferralCodeSpec(request.ReferrerCode), cancellationToken);
            
            var passwordHash = cryptographyService.Hash(request.Password);
            var user = AppUser.Create(new AppUserCreateParams(keycloakIdUser.Value, normalizedEmail, passwordHash,
                whoInvited?.Id, false, nameof(AuthProviders.LocalProvider)));
            await userRepository.AddAsync(user, cancellationToken);
            
            var apiKey = ApiKey.Create(new ApiKeyCreateParams(user.Id, true));
            await apiKeyRepository.AddAsync(apiKey, cancellationToken);
            //this is currently ned create here, because whe need to be sure the user exists in DB
            var loginAuditDto = new CreateLoginAuditParams
            {
                UserId = user.Id,
                Email = normalizedEmail,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsSuspicious = false, 
                LoggedInAt = DateTime.UtcNow
            };
            var loginAudit = LoginAudit.Create(loginAuditDto);
            loginAuditRepository.AddLogin(loginAudit, cancellationToken);
            
            //Start Saga
            await publishEndpoint.Publish(new UserRegisterSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                ApiKey = apiKey.ApiKeyValue,
                IsActive = true,
                ReferralCode = user.ReferralCode,
                EmailConfirmed = user.EmailConfirmed
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);//need for saga
            await tx.CommitAsync(cancellationToken);

            TokenResponse? tokenResponse = await authService.LoginAsync(normalizedEmail, request.Password);
            if (tokenResponse is null)
                return Result<RegisterUserResult>.Error("Login failed after registration for user");
            return Result<RegisterUserResult>.Success(new RegisterUserResult
            {
                ApiKey = apiKey.ApiKeyValue,
                Token = tokenResponse,
            });
        }
        catch (DbException e)
        {
            logger.LogError(e, "Database error occurred while handling Provider registration");
            throw;
        }
        catch (Exception e)
        {
            try
            {
                await userAccountService.DeleteUserAsync(normalizedEmail);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to rollback external user creation");
            }

            logger.LogError(e, "Unhandled error while registering user {Email}", MaskedEmail.Create(normalizedEmail));
            throw; //rethrow to middleware handle exception
        }
    }
}