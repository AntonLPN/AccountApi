using Account.Domain.Interfaces;
using MassTransit;

namespace Account.Infrastructure.Services;

public class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : class
    {
        return publishEndpoint.Publish(integrationEvent, ct);
    }
}