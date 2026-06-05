using Account.Domain.Entities;

namespace Account.Domain.Repositories;

public interface ILoginAuditRepository
{
    Task<bool> IsNewDeviceLoginAsync(string userId, string userAgent, CancellationToken cancellationToken = default);
    
    void AddLogin(LoginAudit loginAudit, CancellationToken cancellationToken = default);
}