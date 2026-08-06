using MediatR;

namespace Account.Domain.Events;

public class UserLoggedInDomainEvent : INotification
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public UserLoggedInDomainEvent(string userId, string email, string? ipAddress, string? userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}