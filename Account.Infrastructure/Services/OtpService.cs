using Account.Application.Features.Account.OtpCodeVerification;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Specifications;
using Ardalis.Result;
using Ardalis.SharedKernel;
using Microsoft.Extensions.Logging;
using OtpNet;

// ReSharper disable InconsistentNaming

namespace Account.Infrastructure.Services;

public class OtpService(
    ICryptography cryptographyService,
    IRepository<OtpSessions> otpSessionRepository,
    ILogger<OtpService> logger) : IOtpService
{
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

    public async Task<Result<OtpSessions>> ValidateActiveSessionAsync(AppUser user, string otpCode,
        CancellationToken cancellationToken = default)
    {
        var otpCodeHash = cryptographyService.Hash(otpCode);
        var otpActiveSession =
            await otpSessionRepository.FirstOrDefaultAsync(new OtpGetActiveSessionSpec(user.Id, otpCodeHash),
                cancellationToken);
        if (otpActiveSession == null || otpActiveSession.UsedAt != null)
            return Result<OtpSessions>.NotFound(
                "No active OTP session found for the user or OTP already used");

        if (otpActiveSession.ExpiresAt < DateTime.UtcNow)
        {
            logger.LogWarning("OTP session expired for user {UserId}", user.Id);
            return Result<OtpSessions>.Conflict("OTP session expired");
        }

        var isVerified = VerifyOtpCode(user, otpCode);
        if (!isVerified)
        {
            logger.LogWarning("Invalid OTP attempt for user {UserId}", user.Id);
            return Result<OtpSessions>.Conflict("Invalid OTP code");
        }
        return Result<OtpSessions>.Success(otpActiveSession);
    }
}