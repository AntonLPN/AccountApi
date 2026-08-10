using Account.Infrastructure.Configuration;
using Account.Infrastructure.Consumers.Register;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.TwoFactor;
using Account.Infrastructure.Saga.UserRegister;
using MassTransit;

namespace AccountApi.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitMessaging(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            RegisterSagas(x);
            //Set up MassTransit consumers and outbox
            x.AddConsumers(typeof(SendWelcomeEmailConsumer)
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
}