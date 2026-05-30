using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;

namespace Account.Infrastructure.Saga;

public class UserRegistrationSagaDefinition : SagaDefinition<UserRegistrationSagaState>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<UserRegistrationSagaState> sagaConfigurator,
        IRegistrationContext context)
    {
        //Retry logic
        endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 1000, 5000));
        //Circuit Breaker
        endpointConfigurator.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1); // period of time to track failures
            cb.TripThreshold = 10; // percentage of failures to trip the circuit
            cb.ActiveThreshold = 5; // number of requests before the circuit can be tripped
            cb.ResetInterval = TimeSpan.FromSeconds(30); // time to wait before resetting the circuit
        });
        // 3. Rate Limiting (example: 50 messages per second)
        endpointConfigurator.UseRateLimit(50, TimeSpan.FromSeconds(1));
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}