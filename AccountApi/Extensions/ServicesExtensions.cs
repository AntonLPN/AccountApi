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
    public static IServiceCollection AddMySqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("DbConfig:ConnectionString").Value;
        ArgumentException.ThrowIfNullOrEmpty(connectionString,
            "Database connection string configuration is missing or empty.");
        // var version = configuration.GetSection("DbConfig:VersionMySql").Value;
        // ArgumentException.ThrowIfNullOrEmpty(version, "Database version configuration is missing or empty.");
        var serverVersion = ServerVersion.AutoDetect(connectionString); // in this case catch exception when use docker
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySqlOptions => { mySqlOptions.CommandTimeout(30); })
        );

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSettings = configuration.GetSection("Authentication:Schemes:Bearer");
        var preAuthKey = configuration["Authentication:PreAuth:SigningKey"]
                         ?? throw new InvalidOperationException(
                             "Authentication:PreAuth:SigningKey configuration is missing.");
        services.AddAuthentication("Bearer")
            //Keycloak - main authentication scheme, for all endpoints except /verify-otp
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = keycloakSettings["Authority"];
                options.Audience = keycloakSettings["ValidAudience"];
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
#if DEBUG
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"Bearer auth FAILED: {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
#endif
            })
            //PreAuth - second authentication scheme, for /verify-otp
            .AddJwtBearer("PreAuth", options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "account-api-preauth",
                    ValidateAudience = true,
                    ValidAudience = "account-api-preauth",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(preAuthKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
#if DEBUG
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"PreAuth FAILED: {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
#endif
            })
            .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(AuthPolicies.ApiKeyOnly, _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.ApiKeyOnly, policy => policy
                .AddAuthenticationSchemes(AuthPolicies.ApiKeyOnly)
                .RequireAuthenticatedUser())

            //for /verify-otp
            .AddPolicy(AuthPolicies.PreAuthOnly, policy => policy
                .AddAuthenticationSchemes("PreAuth")
                .RequireAuthenticatedUser()
                .RequireClaim("purpose",
                    "otp_pending"))

            //for all other endpoints, require full authentication through Keycloak
            .AddPolicy(AuthPolicies.MfaRequired, policy => policy
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser())
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

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

    public static IServiceCollection AddMassTransitMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            RegisterSagas(x);
            //Set up MassTransit consumers and outbox
            x.AddConsumers(typeof(AppDbContext)
                .Assembly); //IConsumer implementations for MassTransit Outbox
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UseMySql();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(5);
                o.QueryTimeout = TimeSpan.FromSeconds(30);
            });
            var useRabbit = configuration.GetValue<bool>("Messaging:UseRabbitMq");
            if (useRabbit)
            {
                // Set up RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitConfig = configuration.GetSection("Messaging:RabbitMq").Get<RabbitMqConfig>() ??
                                       throw new InvalidOperationException(
                                           "RabbitMq configuration is missing or invalid.");

                    // Override host from environment variable if set (for Docker)
                    var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? rabbitConfig.Host;

                    cfg.Host(rabbitHost, rabbitConfig.Port, rabbitConfig.VirtualHost, h =>
                    {
                        h.Username(rabbitConfig.Username);
                        h.Password(rabbitConfig.Password);
                    });
                    cfg.UseMessageRetry(r =>
                    {
                        r.Handle<TimeoutException>();
                        r.Handle<HttpRequestException>();
                        r.Interval(3, TimeSpan.FromSeconds(5));
                    });

                    cfg.ConfigureEndpoints(context); //important for saga
                });
            }
            else //for debug only if not use RabbitMq
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r =>
                    {
                        r.Handle<TimeoutException>();
                        r.Handle<HttpRequestException>();
                        r.Interval(3, TimeSpan.FromSeconds(5));
                    });

                    cfg.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromMinutes(1),
                        TimeSpan.FromMinutes(5),
                        TimeSpan.FromMinutes(15)));

                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }

    private static void RegisterSagas(IBusRegistrationConfigurator x)
    {
        x.SetEntityFrameworkSagaRepositoryProvider(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.ExistingDbContext<AppDbContext>();
            r.UseMySql();
        });
        x.AddSagaStateMachine<UserRegistrationSaga, UserRegistrationSagaState, UserRegistrationSagaDefinition>();
        x.AddSagaStateMachine<TwoFactorSaga, TwoFactorSagaState, TwoFactorSagaDefinition>();
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