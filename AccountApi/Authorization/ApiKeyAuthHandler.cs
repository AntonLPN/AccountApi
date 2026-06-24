using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AccountApi.Authorization;

public class ApiKeyAuthSchemeOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConnectionMultiplexer redis,
    IOptions<ApiKeyOptions> apiKeyOptions,
    IOptions<RedisOptions> redisOptions,
    AppDbContext dbContext)
    : AuthenticationHandler<ApiKeyAuthSchemeOptions>(options, logger, encoder)
{
    private readonly string _masterApiKey = apiKeyOptions.Value.Key;

    private record CachedApiKeyInfo(string UserId, bool IsActive);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = keyHeader.ToString();

        if (!await IsAuthorizedAsync(apiKey))
            return AuthenticateResult.Fail("Invalid or inactive API key.");

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
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
        var raw = await redis.GetDatabase().StringGetAsync(apiKey);
        if (raw.IsNullOrEmpty) return null;

        var info = JsonSerializer.Deserialize<CachedApiKeyInfo>(raw!);
        return info?.IsActive;
    }

    private async Task<bool> ValidateFromDbAsync(string apiKey)
    {
        var key = await dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.ApiKeyValue == apiKey);

        if (key is not null)
            await SetCacheAsync(apiKey, key.IsAuthorize, key.UserId);

        return key is not null && key.IsAuthorize;
    }

    private async Task SetCacheAsync(string apiKey, bool isActive, string userId)
    {
        var payload = JsonSerializer.Serialize(new CachedApiKeyInfo(userId, isActive));
        await redis.GetDatabase()
            .StringSetAsync(apiKey, payload,
                TimeSpan.FromMinutes(redisOptions.Value.CacheStorageTime));
    }
}