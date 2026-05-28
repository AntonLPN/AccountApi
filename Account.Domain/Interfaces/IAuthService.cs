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
    /// <returns>id user</returns>
    Task<Result<string>> RegisterUserAsync(string email, string password);
    Task<TokenResponse?> LoginAsync(string email, string password);
}