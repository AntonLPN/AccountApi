namespace Account.Domain.Models;

public record LogoutNotification(
    string ToEmail,
    string? IpAddress,
    DateTime LogoutTime,
    string UserAgent);