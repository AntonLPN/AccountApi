using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public void CreateUser(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user.Email);
        ArgumentNullException.ThrowIfNull(user.PasswordHash);
        var entry = dbContext.Add(user);
    }
}
