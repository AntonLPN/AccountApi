using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class UserByPhoneSpec : Specification<AppUser>, ISingleResultSpecification<AppUser>
{
    public UserByPhoneSpec(string phoneE164)
    {
        Query.Where(u => u.PhoneNumber == phoneE164);
    }
}