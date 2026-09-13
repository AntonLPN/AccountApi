using MediatR;

namespace Account.Domain.Events;

public class PasswordChangedDomainEvent : INotification
{
    public Guid UserId { get; }

    public PasswordChangedDomainEvent(Guid userId)
    {
        UserId = userId;
    }
}