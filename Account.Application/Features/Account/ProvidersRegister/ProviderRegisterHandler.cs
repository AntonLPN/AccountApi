using System.Data.Common;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Enums;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ProvidersRegister;

public class ProviderRegisterHandler(
    ILogger<ProviderRegisterHandler> logger,
    IUserRepository userRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<ProviderRegisterCommand, Result<ProviderRegisterResult>>
{
    public async Task<Result<ProviderRegisterResult>> Handle(ProviderRegisterCommand request,
        CancellationToken cancellationToken)
    {
        string? email = await GetEmailAsync(request.Provider, request.GoogleToken);
        ArgumentException.ThrowIfNullOrEmpty(email);
        try
        {
            if (await userRepository.GetUserByEmailAsync(email, cancellationToken) is not null)
                return Result<ProviderRegisterResult>.Conflict("User already exists");

            var registerResult = await authService.RegisterUserAsync(email, "", false);
            string userId = registerResult.Value;

            var userToken = await authService.LoginByEmailWithoutPasswordAsync(email);
            ArgumentNullException.ThrowIfNull(userToken);

            var whoInvited = await userRepository.FindByReferralCodeAsync(request.ReferrerCode, cancellationToken);
            //Save to DB
            await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
            var user = AppUser.Create(new AppUserCreateParams(userId, email, null, whoInvited?.Id, true,
                nameof(AuthProviders.Google)));

            userRepository.AddUser(user);
            var apiKey = apiKeyRepository.CreateApiKey(user.Id);
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

            return Result<ProviderRegisterResult>.Success(new ProviderRegisterResult
            {
                ApiKey = apiKey,
                Token = userToken,
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
                await authService.DeleteUserByEmailAsync(email);
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Failed to rollback external user creation");
            }

            logger.LogError(e, "Error occurred while handling GoogleRegisterCommand");
            throw;
        }
    }

    private async Task<string?> GetEmailAsync(AuthProviders provider, string token)
    {
        switch (provider)
        {
            case AuthProviders.Google:
                var googlePayload = await authService.GoogleValidateAsync(token);
                return googlePayload.Email;
            case AuthProviders.Apple:
                //TODO waiting for apple implementation
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return null;
    }
}