using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class UserByIdSpec:Specification<AppUser>,ISingleResultSpecification<AppUser>
{
    public UserByIdSpec(string userId)
    {
        Query.Where(u => u.Id == userId);
    }
}