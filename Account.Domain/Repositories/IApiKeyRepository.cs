namespace Account.Domain.Repositories;

public interface IApiKeyRepository
{
    string CreateApiKey(string userId);
    Task<string?> GetApiKeyAsync(string userId, CancellationToken cancellationToken = default);
}