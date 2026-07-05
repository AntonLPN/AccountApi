using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;

namespace Account.Infrastructure.Repositories;

public class OtpSessionRepository(AppDbContext dbContext):IOtpSessionRepository
{
    public void AddOtpSession(OtpSessions createParams)
    {
        ArgumentNullException.ThrowIfNull(createParams, nameof(createParams));
        ArgumentException.ThrowIfNullOrEmpty(createParams.CodeHash, nameof(createParams.CodeHash));
        ArgumentException.ThrowIfNullOrEmpty(createParams.UserId, nameof(createParams.UserId));
        
        dbContext.OptSessions.Add(createParams);
    }
}