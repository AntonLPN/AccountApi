using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Repositories;

public class OtpSessionRepository(AppDbContext dbContext) : IOtpSessionRepository
{
    public void AddOtpSession(OtpSessions createParams)
    {
        ArgumentNullException.ThrowIfNull(createParams, nameof(createParams));
        ArgumentException.ThrowIfNullOrEmpty(createParams.CodeHash, nameof(createParams.CodeHash));
        ArgumentException.ThrowIfNullOrEmpty(createParams.UserId, nameof(createParams.UserId));

        dbContext.OptSessions.Add(createParams);
    }

    public Task<OtpSessions?> GetActiveOtpSessionAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));
        return dbContext.OptSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.UsedAt == null,
            cancellationToken);
    }

    public void DeleteOtpSession(OtpSessions otpSession)
    {
        ArgumentNullException.ThrowIfNull(otpSession, nameof(otpSession));
        dbContext.OptSessions.Remove(otpSession);
    }

    public void UpdateOtpSession(OtpSessions otpSession)
    {
        ArgumentNullException.ThrowIfNull(otpSession, nameof(otpSession));
        dbContext.OptSessions.Update(otpSession);
    }
}