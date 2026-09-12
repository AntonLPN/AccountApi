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
    AppDbContext dbContext,
    ICryptography cryptographyService)
    : AuthenticationHandler<ApiKeyAuthSchemeOptions>(options, logger, encoder)
{
    private readonly string _masterApiKey = apiKeyOptions.Value.Key;

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record CachedApiKeyInfo(string UserId, bool IsActive);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
            return AuthenticateResult.NoResult();

        var apiKey = keyHeader.ToString();

        if (!string.IsNullOrEmpty(_masterApiKey) && apiKey == _masterApiKey)
        {
            return AuthenticateResult.NoResult();
        }

        if (!await IsAuthorizedAsync(apiKey))
            return AuthenticateResult.Fail("Invalid API key");

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<bool> IsAuthorizedAsync(string apiKey)
    {
        var key = await dataCache.GetAsync<CachedApiKeyInfo>(apiKey);
        if (key != null)
            return key.IsActive;

        return await ValidateFromDbAsync(apiKey);
    }

    private async Task<bool> ValidateFromDbAsync(string apiKey)
    {
        var hashedApiKey = cryptographyService.Hash(apiKey);
        var key = await dbContext.ApiKeys.Include(k => k.AppUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.HashApiKey == hashedApiKey && k.IsAuthorize && !k.IsDeleted);
        if (key == null || key.AppUser is { IsBlocked: true })
            return false;
        
        await SetCacheAsync(hashedApiKey, key.IsAuthorize, key.UserId.ToString());
        return true;
    }

    private async Task SetCacheAsync(string apiKey, bool isActive, string userId)
    {
        var payload = JsonSerializer.Serialize(new CachedApiKeyInfo(userId, isActive));
        await dataCache.SetAsync(apiKey, payload, TimeSpan.FromMinutes(5));
    }
}