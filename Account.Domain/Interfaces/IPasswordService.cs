using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IPasswordService
{
    Task<Result> ChangePasswordAsync(string email, string newPassword);
}