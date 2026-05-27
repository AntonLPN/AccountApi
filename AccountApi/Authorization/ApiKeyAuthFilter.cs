using Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AccountApi.Authorization;

public class ApiKeyAuthFilter(
    IConfiguration configuration,
    ILogger<ApiKeyAuthFilter> logger,
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache)
    : IAsyncAuthorizationFilter
{
    private readonly List<string> _apiKeys =
        configuration.GetSection("ApiSettings:ApiKeys").Get<List<string>>() ??
        throw new InvalidOperationException("ApiSettings:ApiKeys configuration section is missing or invalid.");

    private record CachedApiKeyInfo(string UserId, bool IsActive);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
                return;

            if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey) ||
                string.IsNullOrWhiteSpace(extractedApiKey.ToString()))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var apiKey = extractedApiKey.ToString();
            if (_apiKeys.Contains(apiKey))
                return;

            var cacheKey = $"auth_key_{apiKey}";
            if (memoryCache.TryGetValue(cacheKey, out CachedApiKeyInfo? cachedInfo))
            {
                if (cachedInfo is not null && cachedInfo.IsActive) return;
                context.Result = new UnauthorizedResult();
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var key = await dbContext.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.ApiKeyValue == apiKey);

            if (key is null || !key.IsAuthorize)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Cache only successful auth results
            if (key.UserId != null)
            {
                var infoToCache = new CachedApiKeyInfo(key.UserId, IsActive: true);

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
                    .SetPriority(CacheItemPriority.High);

                memoryCache.Set(cacheKey, infoToCache, cacheOptions);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "ApiKeyAuthFilter: Error checking API key. Error: {ErrorMessage}", e.Message);
            context.Result = new UnauthorizedResult();
        }
    }
}