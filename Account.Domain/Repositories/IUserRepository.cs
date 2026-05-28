using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    void CreateUser(AppUser user, CancellationToken cancellationToken = default);
}