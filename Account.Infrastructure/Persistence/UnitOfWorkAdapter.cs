using Account.Domain;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace Account.Infrastructure.Persistence;

// ReSharper disable once ClassNeverInstantiated.Global
public class UnitOfWorkAdapter(AppDbContext dbContext, IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var affected = await dbContext.SaveChangesAsync(cancellationToken);
        
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0);
        
        await domainEventDispatcher.DispatchAndClearAsync(aggregates, cancellationToken);
        var affectedByEvents = await dbContext.SaveChangesAsync(cancellationToken); 
        return affected + affectedByEvents;
    }

    public async Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfTx(tx);
    }
    
    private class EfTx(IDbContextTransaction tx) : IAppDbTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => tx.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => tx.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
