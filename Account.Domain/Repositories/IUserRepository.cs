using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
    void AddUser(AppUser user);
    /// <summary>
    /// This method is used to get the user by referral read-only purpose
    /// </summary>
    /// <param name="referralCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AppUser?> GetUserByReferralCodeAsReadOnlyAsync(string referralCode, CancellationToken cancellationToken = default);
}