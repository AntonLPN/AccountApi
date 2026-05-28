namespace Account.Domain.Repositories;

public interface IUnitOfWork
{
    /// <summary>
    /// Begins a new database transaction. The returned IAppDbTransaction should be disposed after use to ensure proper cleanup of resources.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves all changes made in the context to the database. This method should be called after making changes to
    /// the entities tracked by the context to persist those changes to the database.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IAppDbTransaction : IAsyncDisposable
{
    /// <summary>
    ///  Commits the current transaction, saving all changes made during the transaction to the database.
    /// If any errors occur during the commit process, the transaction will be rolled back to maintain data integrity.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>
    ///  Rolls back the current transaction, undoing all changes made during the transaction.
    /// This method should be called if any errors occur during the transaction to ensure that the database
    /// remains in a consistent state.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RollbackAsync(CancellationToken cancellationToken);
}