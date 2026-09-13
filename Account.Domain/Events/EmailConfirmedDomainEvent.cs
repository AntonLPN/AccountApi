using MediatR;

namespace Account.Domain.Events;

public sealed class EmailConfirmedDomainEvent : INotification
{
    public Guid UserId { get; set; }

    public EmailConfirmedDomainEvent(Guid userId)
    {
        UserId = userId;
    }
}