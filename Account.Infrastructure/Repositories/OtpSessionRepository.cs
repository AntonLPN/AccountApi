using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Repositories;

public class OtpSessionRepository(AppDbContext dbContext, ICryptography cryptographyService) : IOtpSessionRepository
{
    public void AddOtpSession(OtpSessions createParams)
    {
        ArgumentNullException.ThrowIfNull(createParams, nameof(createParams));
        ArgumentException.ThrowIfNullOrEmpty(createParams.CodeHash, nameof(createParams.CodeHash));
        ArgumentException.ThrowIfNullOrEmpty(createParams.UserId, nameof(createParams.UserId));

        dbContext.OptSessions.Add(createParams);
    }

    public async Task InvalidateActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

        var activeSessions = await dbContext.OptSessions
            .Where(s => s.UserId == userId && s.UsedAt == null && s.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var session in activeSessions)
        {
            session.InvalidatedAt = now;
        }
    }

    public Task<OtpSessions?> GetActiveOtpSessionAsync(string userId, string otpCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));
        var otpCodeHash = cryptographyService.Hash(otpCode);
        return dbContext.OptSessions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.UsedAt == null && s.CodeHash == otpCodeHash,
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