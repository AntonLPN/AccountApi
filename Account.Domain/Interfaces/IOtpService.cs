using Account.Domain.Entities;
using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IOtpService
{
    string GenerateOtpCode(AppUser user);
    bool VerifyOtpCode(AppUser user, string otpCode);

    Task<Result<bool>> InvalidateOtpSessionsAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result<bool>> CreateOtpSessionAsync(string userId, string otpCode, Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<OtpSessions>> ValidateActiveSessionAsync(
        AppUser user,
        string otpCode,
        CancellationToken cancellationToken = default);
}