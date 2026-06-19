using System.Data.Common;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.DTOs.EntitiesDTO;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
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
    IUserRepository userRepository,
    IApiKeyRepository apiKeyRepository,
    ICryptography cryptographyService,
    IPublishEndpoint publishEndpoint,
    ILoginAuditRepository  loginAuditRepository)
    : ICommandHandler<RegisterCommand, Result<RegisterUserResult>>
{
    public async Task<Result<RegisterUserResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userByEmail = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken);
        if (userByEmail is not null)
            return Result<RegisterUserResult>.Conflict("User already exists");

        var keycloakIdUser = await authService.RegisterUserAsync(request.Email, request.Password);
        if (!keycloakIdUser.IsSuccess)
            return Result<RegisterUserResult>.Error(keycloakIdUser.Errors.FirstOrDefault() ?? "Registration failed");
        
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var whoInvited = await userRepository.FindByReferralCodeAsync(request.ReferrerCode, cancellationToken);
            var passwordHash = cryptographyService.Hash(request.Password);
            var user = AppUser.Create(new AppUserCreateParams(keycloakIdUser.Value, request.Email, passwordHash,
                whoInvited?.Id, false, nameof(AuthProviders.LocalProvider)));

            userRepository.AddUser(user);
            var apiKey = apiKeyRepository.CreateApiKey(user.Id);
            //this is currently ned create here, because whe need to be sure the user exists in DB
            var loginAuditDto = new CreateLoginAuditDto
            {
                UserId = user.Id,
                Email = request.Email,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsSuspicious = false, 
                LoggedInAt = DateTime.UtcNow
            };
            var loginAudit = LoginAudit.Create(loginAuditDto);
            loginAuditRepository.AddLogin(loginAudit, cancellationToken);
            
            //Start Saga
            await publishEndpoint.Publish(new UserSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                ApiKey = apiKey
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            TokenResponse? tokenResponse = await authService.LoginAsync(request.Email, request.Password);
            if (tokenResponse is null)
                return Result<RegisterUserResult>.Error("Login failed after registration for user");
            return Result<RegisterUserResult>.Success(new RegisterUserResult
            {
                ApiKey = apiKey,
                Token = tokenResponse,
            });
        }
        catch (DbException e)
        {
            logger.LogError(e, "Database error occurred while handling GoogleRegisterCommand");
            throw;
        }
        catch (Exception e)
        {
            try
            {
                await authService.DeleteUserByEmailAsync(request.Email);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to rollback external user creation");
            }

            var safeEmail = MaskedEmail.Create(request.Email);
            logger.LogError(e, "Unhandled error while registering user {Email}", safeEmail);
            throw; //rethrow to middleware handle exception
        }
    }
}