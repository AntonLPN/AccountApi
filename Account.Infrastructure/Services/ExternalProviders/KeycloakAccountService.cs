using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Ardalis.Result;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Account.Infrastructure.Services.ExternalProviders;

public class KeycloakAccountService(
    KeycloakHttpClient keycloakHttpClient,
    IOptions<KeycloakAdminOptions> keyCloakOptions,
    IDistributedCache cache)
    : IUserAccountService
{
    private const string CacheKey = "keycloak_admin_token";
    
    public async Task<Result<string>> RegisterUserAsync(string email, string? password, bool useCredentials = true)
    {
      
        var adminToken = await cache.GetStringAsync(CacheKey);

        if (string.IsNullOrEmpty(adminToken))
        {
            var tokenResponse = await keycloakHttpClient.GetAdminTokenAsync(keyCloakOptions.Value);
            adminToken = tokenResponse?.AccessToken;
            if (string.IsNullOrEmpty(adminToken) || tokenResponse is null)
            {
                return Result<string>.Error("Failed to obtain admin token");
            }

            await cache.SetStringAsync(
                CacheKey,
                adminToken,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60)
                });
        }

        return await keycloakHttpClient.RegisterUserAsync(
            userName: email,
            email: email,
            password: password,
            adminToken,
            keyCloakOptions.Value, useCredentials);
    }

    public Task<Result> DeleteUserAsync(string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        return keycloakHttpClient.DeleteUserByEmailAsync(email, keyCloakOptions.Value);
    }
}