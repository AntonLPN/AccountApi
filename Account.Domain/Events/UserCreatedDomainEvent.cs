using MediatR;

namespace Account.Domain.Events;

public sealed class UserCreatedDomainEvent : INotification
{
    public string UserId { get; set; }
    public string Email { get; set; } = "";

    public UserCreatedDomainEvent(string userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}