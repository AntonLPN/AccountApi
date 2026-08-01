using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IUserAccountService
{
    /// <summary>
    /// Register a new user in the thirds party service and generate userId
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="useCredentials"></param>
    /// <returns>uniq userId as Result object</returns>
    Task<Result<string>> RegisterUserAsync(string email, string? password,bool useCredentials = true);
    Task<Result> DeleteUserAsync(string email);

}