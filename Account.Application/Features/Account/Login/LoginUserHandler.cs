using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using Account.Domain.ValueObjects;
using Ardalis.Result;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Account.Application.Features.Account.Login;

public class LoginUserHandler(
    ILogger<LoginUserHandler> logger,
    IAuthService authService,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IApiKeyRepository apiKeyRepository,
    IPublishEndpoint publishEndpoint)
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
                //TODO implement mfa
                var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
                var totp = new Totp(secretKey, step: 300, mode: OtpHashMode.Sha1, totpSize: 6);
                var otpCode = totp.ComputeTotp();
                
                
                
                throw new  NotImplementedException();
                return Result<LoginUserResult>.Success(new LoginUserResult()
                {
                    IsMfaRequired = true,
                    MfaStateToken = "IMPLEMENT TOKEN LOGIC HERE"
                });
            }           

            var apiKey = await apiKeyRepository.GetApiKeyByUserIdAsync(user.Id);

            await publishEndpoint.Publish(new UserLoginSagaStartedIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = user.Id,
                Email = user.Email,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);//need for saga

            logger.LogInformation("User {Email} logged in, login saga started", MaskedEmail.Create(request.Email));

            return Result<LoginUserResult>.Success(new LoginUserResult
            {
                ApiKey = apiKey ?? "",
                Token = tokenResponse
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling LoginCommand for email {Email}", MaskedEmail.Create(request.Email));
            throw;
        }

    }
}