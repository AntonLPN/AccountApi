using Account.Domain.Interfaces;
using MassTransit;

namespace Account.Infrastructure.Services;

public class MassTransitOutboxEventPublisher(
    IPublishEndpoint publishEndpoint)
    : IOutboxEventPublisher
{
    public async Task AddOutboxEventAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        await publishEndpoint.Publish(@event, cancellationToken);
    }
}