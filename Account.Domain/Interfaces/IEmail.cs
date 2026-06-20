using Account.Domain.DTOs;
using Account.Domain.Models;

namespace Account.Domain.Interfaces;

public interface IEmail
{
    Task<bool> SendWelcomeEmail(string toEmail, CancellationToken cancellationToken = default);

    Task<bool> SendNewDeviceLoginEmail(SuspiciousDevice suspiciousDevice,
        CancellationToken cancellationToken = default);

    Task<bool> SendLogoutNotificationEmail(LogoutNotification logoutNotification,
        CancellationToken cancellationToken = default);
}