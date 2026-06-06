using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;

namespace Account.Infrastructure.Repositories;

public class LogoutAuditRepository(AppDbContext dbContext) : ILogoutAuditRepository
{
    public void AddLogout(LogoutAudit logoutAudit, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(logoutAudit.UserId, nameof(logoutAudit));
        dbContext.LogoutAudits.Add(logoutAudit);
    }
}