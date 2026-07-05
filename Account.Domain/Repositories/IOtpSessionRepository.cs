using Account.Domain.Entities;
using Account.Domain.Models;

namespace Account.Domain.Repositories;

public interface IOtpSessionRepository
{
    public void AddOtpSession(OtpSessions createParams);
}