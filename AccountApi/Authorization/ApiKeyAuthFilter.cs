using System.Text.Json;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AccountApi.Authorization;

public class ApiKeyAuthFilter(
    ILogger<ApiKeyAuthFilter> logger,
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<ApiKeyOptions> apiKeyOptions,
    IOptions<RedisOptions> redisOptions,
    AppDbContext dbContext)
    : IAsyncAuthorizationFilter
{
    private readonly string _masterApiKey = apiKeyOptions.Value.Key;
    private record CachedApiKeyInfo(string UserId, bool IsActive);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            if (IsAnonymousEndpoint(context)) return;

            var apiKey = ExtractApiKeyFromHeader(context.HttpContext);
            if (apiKey is null || !await IsAuthorizedAsync(apiKey))
                context.Result = new UnauthorizedResult();

        }
        catch (Exception e)
        {
            logger.LogError(e, "ApiKeyAuthFilter: Error checking API key. Error: {ErrorMessage}", e.Message);
            throw;
        }
    }

    private static bool IsAnonymousEndpoint(AuthorizationFilterContext context) =>
        context.HttpContext.GetEndpoint()
            ?.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null;

    private string? ExtractApiKeyFromHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var key) &&
            !string.IsNullOrWhiteSpace(key))
            return key.ToString();

        logger.LogWarning("ApiKeyAuthFilter: API key missing from request headers.");
        return null;
    }

    private async Task<bool> IsAuthorizedAsync(string apiKey)
    {
        if (apiKey.Equals(_masterApiKey)) return true;

        var cached = await GetFromCacheAsync(apiKey);
        if (cached.HasValue) return cached.Value;

        return await ValidateFromDbAsync(apiKey);
    }

    private async Task<bool?> GetFromCacheAsync(string apiKey)
    {
        var raw = await connectionMultiplexer.GetDatabase().StringGetAsync(apiKey);
        if (raw.IsNullOrEmpty) return null;

        var info = JsonSerializer.Deserialize<CachedApiKeyInfo>(raw!);
        return info?.IsActive;
    }

    private async Task<bool> ValidateFromDbAsync(string apiKey)
    {
        var key = await dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.ApiKeyValue == apiKey);

        if (key is null || !key.IsAuthorize) return false;

        await SetCacheAsync(apiKey, key.IsAuthorize, key.UserId);
        return true;
    }

    private async Task SetCacheAsync(string apiKey, bool isActive, string userId)
    {
        var payload = JsonSerializer.Serialize(new CachedApiKeyInfo(userId, isActive));
        await connectionMultiplexer.GetDatabase()
            .StringSetAsync(apiKey, payload, TimeSpan.FromMinutes(redisOptions.Value.CacheStorageTime));
    }
}