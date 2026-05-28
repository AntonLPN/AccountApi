namespace Account.Domain.Repositories;

public interface IApiKeyRepository
{
    string CreateApiKey(string userId);
    Task<string?> GetApiKeyByUserIdAsync(string userId);
}