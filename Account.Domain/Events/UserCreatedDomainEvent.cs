using System.Text.Json.Nodes;
using MediatR;

namespace Account.Domain.Events;

public sealed class UserCreatedDomainEvent : INotification
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public JsonObject? Metadata { get; }

    public UserCreatedDomainEvent(
        Guid userId,
        string email,
        string? ipAddress,
        string? userAgent,
        JsonObject? metadata = null)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Metadata = metadata;
    }
}