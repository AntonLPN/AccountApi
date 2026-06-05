using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Repositories;

public class LoginAuditRepository(AppDbContext dbContext, ILogger<LoginAuditRepository> logger) : ILoginAuditRepository
{
    public Task<bool> IsNewDeviceLoginAsync(string userId, string userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent, nameof(userAgent));
        try
        {
            return dbContext.LoginAudits
                .AsNoTracking()
                .AnyAsync(a => a.UserId == userId && a.UserAgent == userAgent,
                    cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to check for new device login for UserId={UserId}", userId);
            throw;
        }
    }
}