using MediatR;

namespace Account.Domain.Events;

public class UserLoggedOutDomainEvent : INotification
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public UserLoggedOutDomainEvent(string userId, string email, string? ipAddress, string? userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}