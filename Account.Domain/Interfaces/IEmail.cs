using Account.Domain.DTOs;

namespace Account.Domain.Interfaces;

public interface IEmail
{
    Task<bool> SendWelcomeEmail(string toEmail, CancellationToken cancellationToken = default);

    Task<bool> SendNewDeviceLoginEmail(SuspiciousDeviceDto suspiciousDeviceDto,
        CancellationToken cancellationToken = default);

    Task<bool> SendLogoutNotificationEmail(LogoutNotificationDto logoutNotificationDto,
        CancellationToken cancellationToken = default);
}