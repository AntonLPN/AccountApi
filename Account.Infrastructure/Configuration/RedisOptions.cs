namespace Account.Infrastructure.Configuration;

public class RedisOptions
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string Password { get; init; } = null!;
    public string User { get; init; } = null!;
    public bool Ssl { get; init; }
    public int CacheStorageTime { get; init; }
}