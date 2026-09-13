using Account.Domain;
using MediatR;
using IDomainEventDispatcher = Account.Domain.Interfaces.IDomainEventDispatcher;

namespace Account.Infrastructure.Services;

public class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAndClearAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken ct)
    {
        var aggregateRoots = aggregates.ToList();
        if (aggregateRoots.Count == 0)
            return;

        var domainEvents = aggregateRoots.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        foreach (var aggregate in aggregateRoots)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, ct);
    }
}