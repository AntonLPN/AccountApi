using MediatR;

namespace Account.Domain.Events;

public class PasswordChangedDomainEvent : INotification
{
    public string UserId { get; set; }

    public PasswordChangedDomainEvent(string userId)
    {
        UserId = userId;
    }
}