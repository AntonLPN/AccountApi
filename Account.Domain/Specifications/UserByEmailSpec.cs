using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class UserByEmailSpec : Specification<AppUser>, ISingleResultSpecification<AppUser>
{
    public UserByEmailSpec(string email)
    {
        Query.Where(u => u.Email == email);
    }
}