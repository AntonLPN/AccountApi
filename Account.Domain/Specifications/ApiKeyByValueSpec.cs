using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Ardalis.Specification;

namespace Account.Domain.Specifications;

public class ApiKeyByValueSpec : Specification<ApiKey>, ISingleResultSpecification<ApiKey>
{
    public ApiKeyByValueSpec(string apiKeyValue)
    {
        
        Query.Where(a => a.KeyPrefix ==  apiKeyValue.Substring(0, 8));
    }
}

    
