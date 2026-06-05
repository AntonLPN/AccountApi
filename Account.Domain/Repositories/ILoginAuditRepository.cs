namespace Account.Domain.Repositories;

public interface ILoginAuditRepository
{
    Task<bool> IsNewDeviceLoginAsync(string userId, string userAgent, CancellationToken cancellationToken = default);
    
}