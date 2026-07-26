using Account.Contracts.Saga.TwoFactor.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;
using OtpNet;

// ReSharper disable InconsistentNaming

namespace Account.Infrastructure.Services;

public class MfaService(
    ICryptography cryptographyService,
    IOtpSessionRepository otpSessionsRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork,
    ILogger<MfaService> logger) : IMfaManager
{
    private const int OTP_CODE_EXPIRATION_TIME = 5;
    private const int OTP_CODE_STEP = 300;
    private const int OTP_CODE_LENGTH = 6;


    public string GenerateOtpCode(AppUser user)
    {
        var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
        var totp = new Totp(secretKey, step: OTP_CODE_STEP, mode: OtpHashMode.Sha1, totpSize: OTP_CODE_LENGTH);
        return totp.ComputeTotp();
    }

    public bool VerifyOtpCode(AppUser user, string otpCode)
    {
        var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
        var totp = new Totp(secretKey, step: OTP_CODE_STEP, mode: OtpHashMode.Sha1, totpSize: OTP_CODE_LENGTH);
        return totp.VerifyTotp(otpCode, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public async Task<string> InitiateTwoFactorProcessAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var otpCode = GenerateOtpCode(user);
        var correlationId = Guid.NewGuid();

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var otpSessions = await otpSessionsRepository.GetActiveSessionsAsync(user.Id, cancellationToken);
            foreach (var session in otpSessions)
            {
                session.Invalidate();
            }

            var otpSessionCreateParams =
                new OtpSessionCreateParams(cryptographyService.Hash(otpCode), user.Id, correlationId);
            var otpSession = OtpSessions.Create(otpSessionCreateParams);
            otpSessionsRepository.AddOtpSession(otpSession);

            await publishEndpoint.Publish(new TwoFactorSagaStartedIntegrationEvent
            {
                CorrelationId = correlationId,
                UserId = user.Id,
                Email = user.Email,
                OtpCode = otpCode,
                ExpirationTime = DateTime.UtcNow.AddMinutes(OTP_CODE_EXPIRATION_TIME)
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return otpCode;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while initiating two-factor process for user {UserId}", user.Id);
            throw;
        }
     
    }
}