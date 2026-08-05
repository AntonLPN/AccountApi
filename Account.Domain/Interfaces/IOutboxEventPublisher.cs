namespace Account.Domain.Interfaces;

public interface IOutboxEventPublisher
{
    /// <summary>
    /// Add an event to the outbox for saga processing
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task AddOutboxEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) 
        where TEvent : class;
}