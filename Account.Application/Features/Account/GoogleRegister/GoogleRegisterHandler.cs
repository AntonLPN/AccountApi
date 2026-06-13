using System.Data.Common;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.GoogleRegister;

public class GoogleRegisterHandler(
    ILogger<GoogleRegisterHandler> logger,
    IUserRepository userRepository,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint,
    ICryptography cryptographyService)
    : ICommandHandler<GoogleRegisterCommand, Result<GoogleRegisterResult>>
{
    public async Task<Result<GoogleRegisterResult>> Handle(GoogleRegisterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var googlePayload = await authService.GoogleValidateAsync(request.GoogleToken);
            var email = googlePayload.Email;
            ArgumentException.ThrowIfNullOrEmpty(email);
            var userId = await authService.GetUserIdByEmailAsync(email);
            if (userId is not null)
                return Result<GoogleRegisterResult>.Conflict("User already exists");

            var registerResult = await authService.RegisterUserAsync(email, "", false);
            userId = registerResult.Value;

            var userToken = await authService.LoginByEmailWithoutPasswordAsync(email);
            ArgumentNullException.ThrowIfNull(userToken);


            var whoInvited = await userRepository.FindByReferralCodeAsync(request.ReferrerCode, cancellationToken);
            //Save to DB
            await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
            var user = AppUser.Create(
                id: userId,
                email: email,
                passwordHash: cryptographyService.Hash(Guid.NewGuid()
                    .ToString()), // Generate a random password hash since it's not used for Google accounts
                referrerId: whoInvited?.Id,
                emailConfirmed: true,
                providerName: "Google");

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

            return Result<GoogleRegisterResult>.Success(new GoogleRegisterResult
            {
                ApiKey = apiKey,
                Token = userToken,
            });
        }
        catch (DbException e)
        {
            //TODO implement delete user if transaction fails
            logger.LogError(e, "Database error occurred while handling GoogleRegisterCommand");
            throw;
        }
        catch (Exception e)
        {
           
            logger.LogError(e, "Error occurred while handling GoogleRegisterCommand");
            throw;
        }
    }

}