using Account.Infrastructure.Configuration;
using StackExchange.Redis;

namespace AccountApi.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSection = configuration.GetSection("Redis").Get<RedisOptions>() ??
                           throw new InvalidOperationException("Redis configuration section is missing.");
        var redisOptions = new ConfigurationOptions
        {
            EndPoints = { { redisSection.Host, redisSection.Port } },
            User = redisSection.User,
            Password = redisSection.Password,
            Ssl = redisSection.Ssl,
            AbortOnConnectFail = false,
            ConnectTimeout = 5000
        };

        var redis = ConnectionMultiplexer.Connect(redisOptions);
        if (!redis.IsConnected)
            throw new InvalidOperationException("Failed to connect to Redis");

        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddStackExchangeRedisCache(options => { options.ConfigurationOptions = redisOptions; });

        return services;
    }
}