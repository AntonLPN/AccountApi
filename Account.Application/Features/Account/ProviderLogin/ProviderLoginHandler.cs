using Account.Application.Interfaces;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.ProviderLogin;

public class ProviderLoginHandler(
    ILogger<ProviderLoginHandler> logger,
    IProviderValidator providerValidator,
    IUserRepository userRepository,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    IAuthService authService)
    : ICommandHandler<ProviderLoginCommand, Result<ProviderLoginResult>>
{
    public async Task<Result<ProviderLoginResult>> Handle(ProviderLoginCommand request,
        CancellationToken cancellationToken)
    {
        string? email =
            await providerValidator.ValidateProviderTokenAndGetEmailAsync(request.Provider, request.ProviderToken);
        ArgumentException.ThrowIfNullOrEmpty(email);
        try
        {
            var user = await userRepository.GetUserByEmailAsync(email, cancellationToken);
            if (user is null)
                return Result<ProviderLoginResult>.Unauthorized();
            var apiKey = await apiKeyRepository.GetApiKeyByUserIdAsync(user.Id);

            var userToken = await authService.LoginAsync(email);
            ArgumentNullException.ThrowIfNull(userToken);

            await publishEndpoint.Publish(new UserLoginSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken); //need for saga

            logger.LogInformation("User {Email} logged in, login saga started", MaskedEmail.Create(email));
            return Result<ProviderLoginResult>.Success(new ProviderLoginResult
            {
                ApiKey = apiKey ?? "",
                Token = userToken
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling GoogleLoginCommand");
            throw;//rethrow to middleware handle exception
        }
    }
}