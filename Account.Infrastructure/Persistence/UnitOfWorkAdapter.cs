using Account.Domain;
using Account.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Account.Infrastructure.Persistence;

// ReSharper disable once ClassNeverInstantiated.Global
public class UnitOfWorkAdapter(AppDbContext dbContext, IPublisher publisher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var affected = await dbContext.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return affected;
    }

    public async Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfTx(tx);
    }

    /// <summary>
    /// Publishes domain events collected by tracked aggregates. Events are cleared before publishing,
    /// so a handler that calls SaveChangesAsync again does not re-publish them.
    /// </summary>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
            return;

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
    }

    private class EfTx(IDbContextTransaction tx) : IAppDbTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => tx.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => tx.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
