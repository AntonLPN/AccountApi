using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class ApiKeyByUserIdSpec : Specification<ApiKey>, ISingleResultSpecification<ApiKey>
{
    public ApiKeyByUserIdSpec(string userId)
    {
        Query.Where(u => u.UserId == userId);
    }
}