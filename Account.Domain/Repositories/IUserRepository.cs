using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    void AddUser(AppUser user);
    
    /// <summary>
    /// Updates the LastLoginAt timestamp for the given user. Returns false if the user was not found.
    /// </summary>
    Task<bool> UpdateLastLoginAsync(string userId, DateTime loggedInAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the LastLogoutAt timestamp for the given user. Returns false if the user was not found.
    /// </summary>
    Task<bool> UpdateLastLogoutAsync(string userId, DateTime loggedOutAt, CancellationToken cancellationToken = default);

}