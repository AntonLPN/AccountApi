using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AccountApi.Authorization;

public class ApiKeyAuthSchemeOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDataCache dataCache,
    IOptions<ApiKeyOptions> apiKeyOptions,
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
        var key = await dataCache.GetAsync<CachedApiKeyInfo>(apiKey);
        if (key != null)
            return key.IsActive;

        return await ValidateFromDbAsync(apiKey);
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
        await dataCache.SetAsync(apiKey, payload);
    }
}