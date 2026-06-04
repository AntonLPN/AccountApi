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
        if (string.IsNullOrEmpty(user.Id))
        {
            throw new InvalidOperationException("You try create user whit empty ID!");
        }
        var entry = dbContext.Add(user);
    }

    public async Task<bool> UpdateLastLoginAsync(string userId, DateTime loggedInAt,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return false;

        user.LastLoginAt = loggedInAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
