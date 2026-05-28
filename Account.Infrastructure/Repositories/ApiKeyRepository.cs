using Account.Domain.Entities;
using Account.Domain.Repositories;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Repositories;

public sealed class ApiKeyRepository(AppDbContext dbContext) : IApiKeyRepository
{
    public string CreateApiKey(string userId)
    {
        var value = Guid.NewGuid().ToString("N");
        
        dbContext.ApiKeys.Add(new ApiKey
        {
            ApiKeyValue = value,
            CreatedAt = DateTime.UtcNow,
            IsAuthorize = true,
            UserId = userId
        });
        return value;    
    }

    public async Task<string?> GetApiKeyByUserIdAsync(string userId)
    {
        var apiKey = await dbContext.ApiKeys.AsNoTracking().Where(k => k.UserId == userId && k.IsAuthorize)
            .FirstOrDefaultAsync();
        return apiKey?.ApiKeyValue;
    }
}