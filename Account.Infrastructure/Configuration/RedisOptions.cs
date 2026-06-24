namespace Account.Infrastructure.Configuration;

public class RedisOptions
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public string User { get; set; }
    public bool Ssl { get; set; }
    public int CacheStorageTime { get; set; }
}