using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;

namespace Account.Infrastructure.Saga.TwoFactor;

public class TwoFactorSagaDefinition : SagaDefinition<TwoFactorSagaState>
{
    public TwoFactorSagaDefinition()
    {
        EndpointName = "two-factor-saga";
    }

    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<TwoFactorSagaState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 1000, 5000));
        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 10;
            cb.ActiveThreshold = 5;
            cb.ResetInterval = TimeSpan.FromSeconds(30);
        });
        endpointConfigurator.UseRateLimit(50, TimeSpan.FromSeconds(1));
        endpointConfigurator.UseEntityFrameworkOutbox<AppDbContext>(context);
    }
}