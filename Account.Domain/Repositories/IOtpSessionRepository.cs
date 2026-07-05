using Account.Domain.Entities;
using Account.Domain.Models;

namespace Account.Domain.Repositories;

public interface IOtpSessionRepository
{
    public void AddOtpSession(OtpSessions createParams);
    public Task<OtpSessions?> GetActiveOtpSessionAsync(string userId, CancellationToken cancellationToken = default);
    public void DeleteOtpSession(OtpSessions otpSession);
    public void UpdateOtpSession(OtpSessions otpSession);
    
}