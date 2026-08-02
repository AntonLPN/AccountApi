using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class LoginAuditByUserAndUserAgentAsReadOnlySpec:Specification<LoginAudit>, ISingleResultSpecification<LoginAudit>
{
    public LoginAuditByUserAndUserAgentAsReadOnlySpec(string userId, string userAgent)
    {
        Query.Where(a => a.UserId == userId && a.UserAgent == userAgent).AsNoTracking();
    }
}