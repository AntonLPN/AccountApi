using System.Text;
using Account.Application.Extensions;
using Account.Application.Features.Account.Register;
using Account.Domain.Extensions;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Extensions;
using Account.Infrastructure.MassTransit;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.TwoFactor;
using Account.Infrastructure.Saga.UserRegister;
using Account.Infrastructure.Services;
using AccountApi.Authorization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(RegisterCommand).Assembly,
            typeof(MassTransitIntegrationEventPublisher).Assembly // for integration events triggers
        ));
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddDomainServices();

        return services;
    }

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