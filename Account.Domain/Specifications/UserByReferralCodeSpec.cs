using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class UserByReferralCodeSpec : Specification<AppUser>, ISingleResultSpecification<AppUser>
{
    public UserByReferralCodeSpec(string referralCode)
    {
        Query.Where(u => u.ReferralCode == referralCode);
    }
}