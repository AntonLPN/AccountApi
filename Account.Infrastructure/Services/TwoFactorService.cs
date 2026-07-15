using Account.Contracts.Saga.TwoFactor.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.Repositories;
using MassTransit;
using OtpNet;

namespace Account.Infrastructure.Services;

public class TwoFactorService(
    ICryptography cryptographyService,
    IOtpSessionRepository otpSessionsRepository,
    IPublishEndpoint publishEndpoint,
    IUnitOfWork unitOfWork) : ITwoFactorManager
{
    public string GenerateOtpCode(AppUser user)
    {
        var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
        var totp = new Totp(secretKey, step: 300, mode: OtpHashMode.Sha1, totpSize: 6);
        return totp.ComputeTotp();
    }

    public bool VerifyOtpCode(AppUser user, string otpCode)
    {
        var secretKey = Convert.FromBase64String(user.EncryptedTwoFactorSecret);
        var totp = new Totp(secretKey, step: 300, mode: OtpHashMode.Sha1, totpSize: 6);
        return totp.VerifyTotp(otpCode, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public async Task<string> InitiateTwoFactorProcessAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var otpCode = GenerateOtpCode(user);
        var correlationId = Guid.NewGuid();

        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

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
            ExpirationTime = DateTime.UtcNow.AddMinutes(5)
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return otpCode;
    }
}