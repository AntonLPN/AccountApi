using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IUserAccountService
{
    Task<Result<string>> RegisterUserAsync(string email, string? password,bool useCredentials = true);
    Task<Result> DeleteUserAsync(string email);

}