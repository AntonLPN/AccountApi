using Account.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Account.Infrastructure.Persistence;

public class UnitOfWorkAdapter(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

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