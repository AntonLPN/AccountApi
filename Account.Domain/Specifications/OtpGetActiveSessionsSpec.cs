using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class OtpGetActiveSessionsSpec : Specification<OtpSessions>
{
    public OtpGetActiveSessionsSpec(string userId)
    {
        Query.Where(s => s.UserId == userId && s.UsedAt == null && s.InvalidatedAt == null);
    }
}