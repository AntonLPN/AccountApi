using Ardalis.Result;

namespace Account.Domain.Interfaces;

public interface IProviderPasswordService
{
    Task<Result> ChangePasswordAsync(string email, string newPassword);
}