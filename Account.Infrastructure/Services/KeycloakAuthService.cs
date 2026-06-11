using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Ardalis.Result;
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

    // ReSharper disable once ConvertToPrimaryConstructor
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

    public async Task<TokenResponse?> GoogleRegisterAsync(string googleToken)
    {
        if (!string.IsNullOrWhiteSpace(googleToken))
            return await _keycloakHttpClient.ExchangeGoogleTokenAsync(googleToken, _options.Value);
        _logger.LogWarning("Google registration attempt with empty token");
        return null;
    }

    public async Task<TokenResponse?> LoginAsync(string email, string password)
    {
        if (!IsValidLoginRequest(email, password))
            return null;

        return await _keycloakHttpClient.LoginAsync(email, password, _options.Value);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;
        return await _keycloakHttpClient.RefreshTokenAsync(refreshToken, _options.Value);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Logout attempt with empty refresh token");
            return false;
        }

        return await _keycloakHttpClient.LogoutAsync(refreshToken, _options.Value);
    }

    public async Task<Result<string>> RegisterUserAsync(string email, string password)
    {
        if (!IsValidRegisterRequest(email, password))
            return Result<string>.Error("Invalid credentials for registration");

        const string cacheKey = "keycloak_admin_token";
        var adminToken = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(adminToken))
        {
            var tokenResponse = await _keycloakHttpClient.GetAdminTokenAsync(_options.Value);
            adminToken = tokenResponse?.AccessToken;
            if (string.IsNullOrEmpty(adminToken) || tokenResponse is null)
            {
                return Result<string>.Error("Failed to obtain admin token");
            }

            await _cache.SetStringAsync(
                cacheKey,
                adminToken,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60)
                });
        }

        return await _keycloakHttpClient.RegisterUserAsync(
            userName: email,
            email: email,
            password: password,
            adminToken,
            _options.Value);
    }


    private bool IsValidLoginRequest(string email, string password)
    {
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) return true;
        _logger.LogWarning("Login attempt with empty credentials");
        return false;
    }

    private bool IsValidRegisterRequest(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            _logger.LogWarning($"Registration attempt with invalid email: {email}");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(password) && password.Length >= 8) return true;
        _logger.LogWarning("Registration attempt with weak password");
        return false;
    }
}