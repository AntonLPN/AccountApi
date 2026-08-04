using Account.Domain.Entities;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class ApiKeyByValueSpec : Specification<ApiKey>, ISingleResultSpecification<ApiKey>
{
    public ApiKeyByValueSpec(string apiKeyValue)
    {
        Query.Where(a => a.ApiKeyValue == apiKeyValue);
    }
}

    
