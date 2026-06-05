using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
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
        try
        {
            var user = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                return false;

            user.LastLoginAt = loggedInAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update last login for UserId={UserId}", userId);
            throw;
        }
    }
    
}