using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
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
    IRepository<AppUser> userRepository,
    IRepository<ApiKey> apiKeyRepository,
    IPublishEndpoint publishEndpoint,
    IMfaManager mfaManager,
    IPreAuthTokenService preAuthTokenService)
    : ICommandHandler<LoginCommand, Result<LoginUserResult>>
{
    public async Task<Result<LoginUserResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = Email.Create(request.Email);
        try
        {
            TokenResponse? tokenResponse = await authService.LoginAsync(normalizedEmail, request.Password);
            if (tokenResponse is null)
                return Result<LoginUserResult>.Unauthorized();
            var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<LoginUserResult>.Unauthorized();
            logger.LogInformation("Start logged for user  {UserId} {DateTime}", user.Id, DateTime.UtcNow);
            if (!user.IsTwoFactorEnabled)
                return await LoginProcess(user, request.IpAddress, request.UserAgent, tokenResponse,
                    cancellationToken);
            var preAuthToken = preAuthTokenService.GeneratePreAuthToken(normalizedEmail);
            return await TwoFactorProcess(user, preAuthToken, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling LoginCommand for email {Email}",
                MaskedEmail.Create(normalizedEmail));
            throw;
        }
    }

    private async Task<LoginUserResult> TwoFactorProcess(AppUser user, string tokenResponse,
        CancellationToken cancellationToken)
    {
        var otpCode = await mfaManager.InitiateTwoFactorProcessAsync(user, cancellationToken);
        //give user temporal access to the app for confirmation otp
        return Result<LoginUserResult>.Success(new LoginUserResult
        {
            IsMfaRequired = true,
            Token = new TokenResponse
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
        var apiKey = await apiKeyRepository.FirstOrDefaultAsync(new ApiKeyByUserIdSpec(user.Id), cancellationToken);
        if (apiKey is null)
            return Result<LoginUserResult>.Error("Failed to generate api key");

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
            ApiKey = apiKey.ApiKeyValue,
            Token = tokenResponse
        });
    }
}