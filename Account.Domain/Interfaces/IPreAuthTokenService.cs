namespace Account.Domain.Interfaces;

public interface IPreAuthTokenService
{
    string GeneratePreAuthToken(string email);
    Task<string> GeneratePendingTokenAsync(string email);
    
    /// <summary>
    /// Validate and at the same time invalidate the pending token
    /// </summary>
    /// <param name="pendingToken"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    Task<bool> ValidateAndConsumePendingTokenAsync(string pendingToken, string email);
}