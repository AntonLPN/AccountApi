using Account.Application.Features.Account.OtpCodeVerification;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Models;
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

    public async Task<Result<bool>> InvalidateOtpSessionsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var otpSessions =
            await otpSessionRepository.ListAsync(new OtpGetActiveSessionsSpec(userId), cancellationToken);
        if (otpSessions.Count == 0)
            return Result<bool>.Success(true);
        foreach (var session in otpSessions)
        {
            session.Invalidate();
        }

        await otpSessionRepository.UpdateRangeAsync(otpSessions, cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CreateOtpSessionAsync(string userId, string otpCode, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var otpSessionCreateParams =
            new OtpSessionCreateParams(cryptographyService.Hash(otpCode), userId, correlationId);
        var otpSession = OtpSessions.Create(otpSessionCreateParams);
        await otpSessionRepository.AddAsync(otpSession, cancellationToken);
        return Result<bool>.Success(true);
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