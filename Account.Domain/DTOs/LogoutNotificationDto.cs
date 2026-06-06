namespace Account.Domain.DTOs;

public record LogoutNotificationDto(
    string ToEmail,
    string? IpAddress,
    DateTime LogoutTime,
    string UserAgent);