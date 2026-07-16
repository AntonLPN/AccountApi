namespace Account.Domain.Interfaces;

public interface IPreAuthTokenService
{
    string GeneratePreAuthToken(string email);
    Task<string> GeneratePendingTokenAsync(string email);
    Task<bool> ValidatePendingTokenAsync(string pendingToken, string email);
}