using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class UserByEmailWithAuthorizedApiKeysSpec : Specification<AppUser>, ISingleResultSpecification<AppUser>
{
    public UserByEmailWithAuthorizedApiKeysSpec(string email)
    {
        Query.Where(u => u.Email == email).Include(u => u.ApiKeys.Where(k => k.IsAuthorize));
    }
}