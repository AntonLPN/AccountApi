using Account.Domain.Entities;
using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IOtpService
{
    string GenerateOtpCode(AppUser user);
    bool VerifyOtpCode(AppUser user, string otpCode);

    Task<Result<OtpSessions>> ValidateActiveSessionAsync(
        AppUser user,
        string otpCode,
        CancellationToken cancellationToken = default);
}