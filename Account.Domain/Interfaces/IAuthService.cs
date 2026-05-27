using Account.Domain.Models;

namespace Account.Domain.Interfaces;

public interface IAuthService
{
    Task<string?> RegisterUserAsync(string email, string password);
    Task<TokenResponse?> LoginAsync(string email, string password);
}