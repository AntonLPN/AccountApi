namespace Account.Domain.DTOs;

public record SuspiciousDeviceDto(
    string ToEmail,
    string DeviceName,
    string? IpAddress,
    DateTime LoginTime,
    string UserAgent);