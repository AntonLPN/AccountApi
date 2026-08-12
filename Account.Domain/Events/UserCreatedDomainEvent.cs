using MediatR;

namespace Account.Domain.Events;

public sealed class UserCreatedDomainEvent : INotification
{
    public string UserId { get; set; }
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public UserCreatedDomainEvent(string userId, string email, string? ipAddress,string? userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}