using Account.Domain.Entities;

namespace Account.Domain.Interfaces;

public interface IMfaManager
{
    string GenerateOtpCode(AppUser user);
    bool VerifyOtpCode(AppUser user, string otpCode);

    Task<string> InitiateTwoFactorProcessAsync(
        AppUser user,
        CancellationToken cancellationToken);
}