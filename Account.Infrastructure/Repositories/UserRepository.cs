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

    public void AddUser(AppUser user)
    {
        ArgumentException.ThrowIfNullOrEmpty(user.Email, nameof(user.Email));
        ArgumentException.ThrowIfNullOrEmpty(user.PasswordHash, nameof(user.PasswordHash));
        ArgumentException.ThrowIfNullOrEmpty(user.Id, nameof(user.Id));

        dbContext.Add(user);
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

    public async Task<bool> UpdateLastLogoutAsync(string userId, DateTime loggedOutAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
                return false;

            user.LastLogoutAt = loggedOutAt;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update last logout for UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<AppUser?> FindByReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(referralCode))
            return null;
        try
        {
            return await dbContext.AppUsers.FirstOrDefaultAsync(u => u.ReferralCode == referralCode, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to find user by referral code={ReferralCode}", referralCode);
            throw;
        }
    }
}