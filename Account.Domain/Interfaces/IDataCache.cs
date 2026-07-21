namespace Account.Domain.Interfaces;

public interface IDataCache
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration =  default);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<T?> ConsumeAsync<T>(string key) where T : class;
    Task<string?> ConsumeAsync(string key); 
    Task SetStringAsync(string key, string value, TimeSpan expiration);
}