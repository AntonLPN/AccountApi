using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Account.Infrastructure.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Account.Infrastructure.Services;

public class KeycloakAuthService : IAuthService
{
    private readonly KeycloakHttpClient _keycloakHttpClient;
    private readonly IOptions<KeycloakAdminOptions> _options;
    private readonly IDistributedCache _cache;
    private readonly ILogger<KeycloakAuthService> _logger;

    public KeycloakAuthService(
        KeycloakHttpClient keycloakHttpClient,
        IOptions<KeycloakAdminOptions> options,
        IDistributedCache cache,
        ILogger<KeycloakAuthService> logger)
    {
        _keycloakHttpClient = keycloakHttpClient;
        _options = options;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TokenResponse?> LoginAsync(string email, string password)
    {
        if (!IsValidLoginRequest(email, password))
            return null;

        return await _keycloakHttpClient.LoginAsync(email, password, _options.Value);
    }

    public async Task<bool> RegisterUserAsync(string email, string password)
    {
        if (!IsValidRegisterRequest(email, password))
            return false;

        const string cacheKey = "keycloak_admin_token";
        var adminToken = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(adminToken))
        {
            adminToken = await _keycloakHttpClient.GetAdminTokenAsync(_options.Value);
            if (string.IsNullOrEmpty(adminToken))
            {
                _logger.LogError("Failed to obtain admin token");
                return false;
            }

            await _cache.SetStringAsync(
                cacheKey,
                adminToken,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(55)
                });
        }

        return await _keycloakHttpClient.RegisterUserAsync(userName: email, email: email, password: password,
            adminToken, _options.Value);
    }

    private bool IsValidLoginRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login attempt with empty credentials");
            return false;
        }

        return true;
    }

    private bool IsValidRegisterRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            _logger.LogWarning($"Registration attempt with invalid email: {email}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            _logger.LogWarning("Registration attempt with weak password");
            return false;
        }

        return true;
    }
}