using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Application.Features.Account.Login;

public class LoginUserHandler(
    ILogger<LoginUserHandler> logger,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint,
    ITwoFactorManager twoFactorManager,
    IPreAuthTokenService preAuthTokenService)
    : ICommandHandler<LoginCommand, Result<LoginUserResult>>
{
    public async Task<Result<LoginUserResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            TokenResponse? tokenResponse = await authService.LoginAsync(request.Email, request.Password);
            if (tokenResponse is null)
                return Result<LoginUserResult>.Unauthorized();

            var user = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                return Result<LoginUserResult>.Unauthorized();

            if (user.IsTwoFactorEnabled)
            {
                var preAuthToken = preAuthTokenService.GeneratePreAuthToken(request.Email);
                return await TwoFactorProcess(user, preAuthToken, cancellationToken);
            }

            return await LoginProcess(user, request.IpAddress, request.UserAgent, tokenResponse,
                cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling LoginCommand for email {Email}",
                MaskedEmail.Create(request.Email));
            throw;
        }
    }

    private async Task<LoginUserResult> TwoFactorProcess(AppUser user, string tokenResponse,
        CancellationToken cancellationToken)
    {
        await twoFactorManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
        //give user temporal access to the app for confirmation otp
        return Result<LoginUserResult>.Success(new LoginUserResult()
        {
            IsMfaRequired = true,
            Token = new TokenResponse()
            {
                AccessToken = tokenResponse,
                RefreshToken = "",
                ExpiresIn = 0,
                TokenType = "pre-auth",
                Scope = ""
            }
        });
    }

    private async Task<LoginUserResult> LoginProcess(AppUser user, string? ipAddress, string? userAgent,
        TokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        var apiKey = await apiKeyRepository.GetApiKeyByUserIdAsync(user.Id);

        await publishEndpoint.Publish(new UserLoginSagaStartedIntegrationEvent
        {
            CorrelationId = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken); //need for saga

        logger.LogInformation("User id  {UserId} logged in {DateTime}, login saga started", user.Id, DateTime.UtcNow);

        return Result<LoginUserResult>.Success(new LoginUserResult
        {
            IsMfaRequired = false,
            ApiKey = apiKey ?? "",
            Token = tokenResponse
        });
    }
}