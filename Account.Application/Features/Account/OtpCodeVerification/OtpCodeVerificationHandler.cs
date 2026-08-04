using Account.Contracts.Saga.TwoFactor.Events;
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
using OtpNet;

namespace Account.Application.Features.Account.OtpCodeVerification;

public class OtpCodeVerificationHandler(
    ILogger<OtpCodeVerificationHandler> logger,
    IRepository<AppUser> userRepository,
    IRepository<OtpSessions> otpSessionRepository,
    IUnitOfWork unitOfWork,
    IAuthService authService,
    IPublishEndpoint publishEndpoint,
    ICryptography cryptographyService)
    : ICommandHandler<OtpCodeVerificationCommand, Result<OtpConfirmationResult>>
{
    public async Task<Result<OtpConfirmationResult>> Handle(OtpCodeVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.OtpCode, nameof(request.OtpCode));

        var normalizedEmail = Email.Create(request.Email);
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(
                new UserByEmailWithAuthorizedApiKeysSpec(normalizedEmail),
                cancellationToken);
            if (user is null)
                return Result<OtpConfirmationResult>.NotFound("User not found");
            //TODO this part should be moved to a separate service otp
            var otpCodeHash = cryptographyService.Hash(request.OtpCode);
            var otpActiveSession =
                await otpSessionRepository.FirstOrDefaultAsync(new OtpGetActiveSessionSpec(user.Id, otpCodeHash),
                    cancellationToken);
            if (otpActiveSession == null || otpActiveSession.UsedAt != null)
                return Result<OtpConfirmationResult>.NotFound(
                    "No active OTP session found for the user or OTP already used");

            if (otpActiveSession.ExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("OTP session expired for user {UserId}", user.Id);
                return Result<OtpConfirmationResult>.Conflict("OTP session expired");
            }

            var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
            var totp = new Totp(secretKey, step: 300, mode: OtpHashMode.Sha1, totpSize: 6);
            bool isValid = totp.VerifyTotp(request.OtpCode, out long timeStepMatched,
                VerificationWindow.RfcSpecifiedNetworkDelay);

            if (!isValid)
            {
                logger.LogWarning("Invalid OTP attempt for user {UserId}", user.Id);
                return Result<OtpConfirmationResult>.Conflict("Invalid OTP code");
            }

            TokenResponse? tokenResponse = await authService.LoginAsync(normalizedEmail);
            if (tokenResponse is null)
                return Result<OtpConfirmationResult>.Unauthorized("Login failed after OTP verification");

            otpActiveSession.UsedAt = DateTime.UtcNow;
            await otpSessionRepository.UpdateAsync(otpActiveSession, cancellationToken);

            await publishEndpoint.Publish(new OtpCodeConfirmedIntegrationEvent()
            {
                CorrelationId = otpActiveSession.CorrelationId,
                UserId = user.Id,
                IsValid = true,
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OTP verification successful for user {UserId}", user.Id);
            return Result<OtpConfirmationResult>.Success(new OtpConfirmationResult()
            {
                ApiKeys = user.ApiKeys.Select(k => k.ApiKeyValue).ToList(),
                Token = tokenResponse,
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during OTP verification for email {Email}", MaskedEmail.Create(normalizedEmail));
            throw; //rethrow to middleware handle exception
        }
    }
}