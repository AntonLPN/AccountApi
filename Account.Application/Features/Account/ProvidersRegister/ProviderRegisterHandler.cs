using System.Data.Common;
using Account.Application.Interfaces;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ProvidersRegister;

public class ProviderRegisterHandler(
    ILogger<ProviderRegisterHandler> logger,
    IRepository<AppUser> userRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IRepository<ApiKey> apiKeyRepository,
    IPublishEndpoint publishEndpoint,
    IProviderValidator providerValidator,
    IRepository<LoginAudit> loginAuditRepository,
    IUserAccountService userAccountService)
    : ICommandHandler<ProviderRegisterCommand, Result<ProviderRegisterResult>>
{
    public async Task<Result<ProviderRegisterResult>> Handle(ProviderRegisterCommand request,
        CancellationToken cancellationToken)
    {
        string? email =
            await providerValidator.ValidateProviderTokenAndGetEmailAsync(request.Provider, request.ProviderToken);
        ArgumentException.ThrowIfNullOrEmpty(email);
        try
        {
            if (await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(email), cancellationToken) is not null)
                return Result<ProviderRegisterResult>.Conflict("User already exists");

            var registerResult = await userAccountService.RegisterUserAsync(email, "", false);
            if (!registerResult.IsSuccess)
                return Result<ProviderRegisterResult>.Error(registerResult.Errors.FirstOrDefault() ??
                                                            "Registration failed");

            string userId = registerResult.Value;

            var userToken = await authService.LoginAsync(email);
            ArgumentNullException.ThrowIfNull(userToken);

            var whoInvited = await userRepository.FirstOrDefaultAsync(new UserByReferralCodeSpec(request.ReferrerCode),
                cancellationToken);
            //Save to DB
            await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
            var user = AppUser.Create(new AppUserCreateParams(userId, email, null, whoInvited?.Id, true,
                nameof(AuthProviders.Google)));
            await userRepository.AddAsync(user, cancellationToken);
            
            var apiKey = ApiKey.Create(new ApiKeyCreateParams(user.Id, true));
            await apiKeyRepository.AddAsync(apiKey, cancellationToken);


            var loginAudit = LoginAudit.Create(new CreateLoginAuditParams
            {
                UserId = user.Id,
                Email = email,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsSuspicious = false,
                LoggedInAt = DateTime.UtcNow
            });
            await loginAuditRepository.AddAsync(loginAudit, cancellationToken);

            //Start Saga
            await publishEndpoint.Publish(new UserRegisterSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                ApiKey = apiKey.ApiKeyValue,
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Result<ProviderRegisterResult>.Success(new ProviderRegisterResult
            {
                ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
                Token = userToken,
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
                await userAccountService.DeleteUserAsync(email);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to rollback external user creation");
            }

            logger.LogError(e, "Error occurred while handling Provider registration");
            throw;
        }
    }
}