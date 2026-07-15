using System.Text.Json;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Account.Infrastructure.Services;

public class RedisDataCache : IDataCache
{
    private readonly IDatabase _database;
    private readonly IOptions<RedisOptions> _redisOptions;

    public RedisDataCache(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions)
    {
        _redisOptions = redisOptions;
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNull)
            return null;
        var deserialized = JsonSerializer.Deserialize<T>(value.ToString());
        return deserialized;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration = default)
    {
        var payload = JsonSerializer.Serialize(value);
        if (expiration == TimeSpan.Zero)
            expiration = TimeSpan.FromMinutes(_redisOptions.Value.CacheStorageTime);
        await _database.StringSetAsync(key, payload, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }
}