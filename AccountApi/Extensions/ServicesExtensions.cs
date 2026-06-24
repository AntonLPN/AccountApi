using Account.Application.Features.Account.Register;
using Account.Domain.Extensions;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Extensions;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.TwoFactor;
using Account.Infrastructure.Saga.UserLogin;
using Account.Infrastructure.Saga.UserLogout;
using Account.Infrastructure.Saga.UserRegister;
using Account.Infrastructure.Services;
using AccountApi.Authorization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddMySqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

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

    public static void AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                var keycloakSettings = configuration.GetSection("Authentication:Schemes:Bearer");
                options.Authority = keycloakSettings["Authority"];
                options.Audience = keycloakSettings["ValidAudience"];
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
#if DEBUG
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var acr = ctx.Principal?.FindFirst("acr")?.Value;
                        Console.WriteLine($"Token OK, acr={acr}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"Auth FAILED: {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnForbidden = ctx =>
                    {
                        var claims = string.Join(", ", ctx.HttpContext.User.Claims.Select(c => $"{c.Type}={c.Value}"));
                        Console.WriteLine($"Forbidden, claims: {claims}");
                        return Task.CompletedTask;
                    }
                };
#endif
            }).AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(AuthPolicies.ApiKeyOnly, _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.ApiKeyOnly, policy => policy  
                .AddAuthenticationSchemes(AuthPolicies.ApiKeyOnly)
                .RequireAuthenticatedUser());
    }

    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(RegisterCommand).Assembly,
            typeof(MassTransitIntegrationEventPublisher).Assembly // for integration events triggers
        ));
        services.AddInfrastructureServices();
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
        x.AddSagaStateMachine<UserLoginSaga, UserLoginSagaState, UserLoginSagaDefinition>();
        x.AddSagaStateMachine<UserLogoutSaga, UserLogoutSagaState, UserLogoutSagaDefinition>();
        x.AddSagaStateMachine<TwoFactorSaga, TwoFactorSagaState, TwoFactorSagaDefinition>();
    }

    public static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        //if user docker compose, redis will be in localhost
        //use this to connect to redis in docker compose
        //var redis = ConnectionMultiplexer.Connect($"{cfg.Host}:{cfg.Port}");
        // var db = redis.GetDatabase();
        //builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

        //if run app without docker compose, for example, https://upstash.com/
        var redisSection = configuration.GetSection("Redis");
        ArgumentNullException.ThrowIfNull(redisSection);
        var redisOptions = new ConfigurationOptions
        {
            EndPoints =
            {
                {
                    redisSection["Host"] ?? throw new InvalidOperationException("Redis config error"),
                    int.Parse(redisSection["Port"] ?? "6379")
                }
            },
            User = redisSection["User"],
            Password = redisSection["Password"],
            Ssl = bool.Parse(redisSection["Ssl"] ?? "false"),
            AbortOnConnectFail = false,
            ConnectTimeout = 5000
        };

        var redis = ConnectionMultiplexer.Connect(redisOptions);
        if (!redis.IsConnected)
            throw new Exception("Failed to connect to Redis");
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisOptions));
    }
}