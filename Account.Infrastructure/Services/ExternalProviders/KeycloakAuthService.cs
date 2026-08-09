using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Account.Infrastructure.Services.ExternalProviders;

public class KeycloakAuthService : IAuthService
{
    private readonly KeycloakHttpClient _keycloakHttpClient;
    private readonly IOptions<KeycloakAdminOptions> _keyCloakOptions;
    private readonly ILogger<KeycloakAuthService> _logger;
    private readonly IOptions<AuthenticationOptions> _authenticationOptions;

    // ReSharper disable once ConvertToPrimaryConstructor
    public KeycloakAuthService(
        KeycloakHttpClient keycloakHttpClient,
        IOptions<KeycloakAdminOptions> keyCloakOptions,
        ILogger<KeycloakAuthService> logger,
        IOptions<AuthenticationOptions> authenticationOptions)
    {
        _keycloakHttpClient = keycloakHttpClient;
        _keyCloakOptions = keyCloakOptions;
        _logger = logger;
        _authenticationOptions = authenticationOptions;
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
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        return await _keycloakHttpClient.RefreshTokenAsync(refreshToken, _keyCloakOptions.Value);
    }
    
    public string GeneratePreAuthToken(string email)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_authenticationOptions.Value.PreAuth.SigningKey));
        int lifeTime = 5;
#if DEBUG
        lifeTime = 60; // For debugging purposes, extend the lifetime to 60 minutes
#endif
        var claims = new[]
        {
            new Claim("email", email),
            new Claim("purpose", "otp_pending")
        };

        var token = new JwtSecurityToken(
            issuer: "account-api-preauth",
            audience: "account-api-preauth",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifeTime), //TTL OTP
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        return await _keycloakHttpClient.LogoutAsync(refreshToken, _keyCloakOptions.Value);
    }

    private bool IsValidLoginRequest(string email, string password)
    {
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) return true;
        _logger.LogWarning("Login attempt with empty credentials");
        return false;
    }
}