using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Repositories;

public sealed class UserRepository(
    AppDbContext dbContext,
    ILogger<UserRepository> logger) : IUserRepository
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

    public async Task<AppUser?> GetUserByReferralCodeAsReadOnlyAsync(string referralCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(referralCode))
            return null;
        try
        {
            return await dbContext.AppUsers.AsNoTracking()
                .FirstOrDefaultAsync(u => u.ReferralCode == referralCode, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to find user by referral code={ReferralCode}", referralCode);
            throw;
        }
    }
}