using Account.Domain.Entities;

namespace Account.Domain.Interfaces;

public interface IMfaManager
{

    Task<string> InitiateTwoFactorProcessAsync(
        AppUser user,
        CancellationToken cancellationToken);
}