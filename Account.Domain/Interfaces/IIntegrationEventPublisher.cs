namespace Account.Domain.Interfaces;

public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to the message bus. The event must be a serializable class that represents the event data.
    /// </summary>
    /// <param name="integrationEvent"></param>
    /// <param name="ct"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : class;
}