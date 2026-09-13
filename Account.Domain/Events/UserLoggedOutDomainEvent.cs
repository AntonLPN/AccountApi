using MediatR;

namespace Account.Domain.Events;

public class UserLoggedOutDomainEvent : INotification
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }

    public UserLoggedOutDomainEvent(Guid userId, string email, string? ipAddress, string? userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}