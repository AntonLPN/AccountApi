using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    void AddUser(AppUser user);

    Task<AppUser?> GetUserByReferralCodeAsReadOnlyAsync(string referralCode, CancellationToken cancellationToken = default);
}