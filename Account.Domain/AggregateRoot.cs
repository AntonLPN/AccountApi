using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace Account.Domain;

public class AggregateRoot
{
    private readonly List<INotification> _domainEvents = new();

    [NotMapped] public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(INotification domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}