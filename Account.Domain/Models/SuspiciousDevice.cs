namespace Account.Domain.Models;

public record SuspiciousDevice(
    string ToEmail,
    string DeviceName,
    string? IpAddress,
    DateTime LoginTime,
    string UserAgent);