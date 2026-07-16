using Account.Domain.Models;

namespace Account.Domain.Interfaces;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(string email);
    Task<TokenResponse?> LoginAsync(string email, string password);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
    string GeneratePreAuthToken(string email);
    Task<bool> LogoutAsync(string refreshToken);
}