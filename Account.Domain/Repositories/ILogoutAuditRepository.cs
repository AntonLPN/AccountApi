using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface ILogoutAuditRepository
{
    void AddLogout(LogoutAudit logoutAudit, CancellationToken cancellationToken = default);
}