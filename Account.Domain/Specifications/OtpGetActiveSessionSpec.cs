using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class OtpGetActiveSessionSpec : Specification<OtpSessions>, ISingleResultSpecification<OtpSessions>
{
    public OtpGetActiveSessionSpec(string userId, string otpCodeHash)
    {
        Query.Where(s => s.UserId == userId &&
                         s.UsedAt == null &&
                         s.IsUsed == false &&
                         s.CodeHash == otpCodeHash);
    }
}