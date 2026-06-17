using Account.Domain.DTOs;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Ardalis.Result;
using Google.Apis.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Account.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly KeycloakHttpClient _keycloakHttpClient;
    private readonly IOptions<KeycloakAdminOptions> _keyCloakOptions;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly IOptions<GoogleOptions> _googleOptions;
    private readonly ICryptography _cryptographyService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AuthService(
        KeycloakHttpClient keycloakHttpClient,
        IOptions<KeycloakAdminOptions> keyCloakOptions,
        IOptions<GoogleOptions> googleOptions,
        IDistributedCache cache,
        ILogger<AuthService> logger,
        ICryptography cryptographyService)
    {
        _keycloakHttpClient = keycloakHttpClient;
        _keyCloakOptions = keyCloakOptions;
        _cache = cache;
        _logger = logger;
        _googleOptions = googleOptions;
        _cryptographyService = cryptographyService;
    }

    public async Task<string?> GoogleRegisterAsync(string email)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            const string cacheKey = "keycloak_admin_token";
            var adminToken = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(adminToken))
            {
                var tokenResponse = await _keycloakHttpClient.GetAdminTokenAsync(_keyCloakOptions.Value);
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
                password: "",
                adminToken: adminToken, // Google registration doesn't require admin token
                _keyCloakOptions.Value);
        }

        _logger.LogWarning("Google registration attempt with empty token");
        return null;
    }

    public Task<string?> GetUserIdByEmailAsync(string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        return _keycloakHttpClient.GetUserIdByEmailAsync(email, _keyCloakOptions.Value);
    }

    public Task<TokenResponse?> LoginAsync(string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        return _keycloakHttpClient.LoginAsync(email, _keyCloakOptions.Value);
    }

    public async Task<TokenResponse?> LoginAsync(string email, string password)
    {
        if (!IsValidLoginRequest(email, password))
            return null;

        return await _keycloakHttpClient.LoginAsync(email, password, _keyCloakOptions.Value);
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;
        return await _keycloakHttpClient.RefreshTokenAsync(refreshToken, _keyCloakOptions.Value);
    }

    public async Task<GooglePayloadDto> GoogleValidateAsync(string googleToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(googleToken);
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience =
            [
                _googleOptions.Value.ClientId
            ]
        };
        try
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
                googleToken,
                settings);

            return new GooglePayloadDto()
            {
                Email = payload.Email,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Google token validation failed");
            throw;
        }
    }

    public Task<Result> DeleteUserByEmailAsync(string email)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        return _keycloakHttpClient.DeleteUserByEmailAsync(email, _keyCloakOptions.Value);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Logout attempt with empty refresh token");
            return false;
        }

        return await _keycloakHttpClient.LogoutAsync(refreshToken, _keyCloakOptions.Value);
    }

    public async Task<Result<string>> RegisterUserAsync(string email, string? password, bool useCredentials = true)
    {
        // if (!IsValidRegisterRequest(email, password))
        //     return Result<string>.Error("Invalid credentials for registration");

        const string cacheKey = "keycloak_admin_token";
        var adminToken = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(adminToken))
        {
            var tokenResponse = await _keycloakHttpClient.GetAdminTokenAsync(_keyCloakOptions.Value);
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
            _keyCloakOptions.Value, useCredentials);
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