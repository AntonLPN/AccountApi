using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;

namespace Account.Infrastructure.Saga.UserLogout;

public class UserLogoutSagaDefinition : SagaDefinition<UserLogoutSagaState>
{
    public UserLogoutSagaDefinition()
    {
        EndpointName = "user-logout-saga";
    }

    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<UserLogoutSagaState> sagaConfigurator,
        IRegistrationContext context)
    {
        //Retry logic
        endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 1000, 5000));
        //Circuit Breaker
        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 10;
            cb.ActiveThreshold = 5;
            cb.ResetInterval = TimeSpan.FromSeconds(30);
        });
        //Rate Limiting
        endpointConfigurator.UseRateLimit(50, TimeSpan.FromSeconds(1));
        endpointConfigurator.UseEntityFrameworkOutbox<AppDbContext>(context);
    }
}