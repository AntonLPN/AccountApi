using MediatR;

namespace Account.Domain.Events;

public sealed class EmailConfirmedDomainEvent : INotification
{
    public  string UserId { get; set; }

    public EmailConfirmedDomainEvent(string userId)
    {
        UserId = userId;
    }
}