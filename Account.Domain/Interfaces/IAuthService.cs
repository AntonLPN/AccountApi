using Account.Domain.DTOs;
using Account.Domain.Models;
using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register user
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="useCredentials"></param>
    /// <returns>id user</returns>
    Task<Result<string>> RegisterUserAsync(string email, string? password,bool useCredentials = true);
    Task<string?> GetUserIdByEmailAsync(string  email);
    Task<TokenResponse?> LoginAsync(string email);
    Task<TokenResponse?> LoginAsync(string email, string password);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<GooglePayloadDto> GoogleValidateAsync(string googleToken);
    Task<Result> DeleteUserByEmailAsync(string email);
    /// <summary>
    /// Logs the user out of Keycloak by revoking the provided refresh token / session.
    /// </summary>
    /// <param name="refreshToken">The refresh token issued at login.</param>
    /// <returns>True if the Keycloak session was successfully revoked.</returns>
    Task<bool> LogoutAsync(string refreshToken);
}