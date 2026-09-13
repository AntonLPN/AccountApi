namespace Account.Domain.Interfaces;

public interface IDomainEventDispatcher
{
    /// <summary>
    /// Publishes domain events collected by tracked aggregates. Events are cleared before publishing,
    /// so a handler that calls SaveChangesAsync again does not re-publish them.
    /// </summary>
    Task DispatchAndClearAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken ct);
}