using Account.Contracts.UserLogin;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class SendLoginNotificationConsumer(
    ILogger<SendLoginNotificationConsumer> logger,
    IEmail emailService)
    : IConsumer<SendLoginNotificationEmailIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SendLoginNotificationEmailIntegrationEvent> context)
    {
        var message = context.Message;

        if (message.IsSuspicious)
        {
            logger.LogInformation("Sending suspicious login notification for UserId={UserId}, Email={Email}",
                message.UserId, MaskedEmail.Create(message.Email));
            var deviceLoginIfo = new SuspiciousDevice(
                message.Email,
                message.UserAgent ?? "Unknown device",
                message.IpAddress,
                DateTime.UtcNow,
                message.UserAgent ?? "Unknown device"
            );
            var sent = await emailService.SendNewDeviceLoginEmail(deviceLoginIfo, context.CancellationToken);
            if (!sent)
            {
                logger.LogWarning("Failed to send suspicious login notification for UserId={UserId}, Email={Email}",
                    message.UserId, MaskedEmail.Create(message.Email));
                return;
            }

            logger.LogInformation("Login notification sent for UserId={UserId}, Email={Email}",
                message.UserId, MaskedEmail.Create(message.Email));
        }
    }
}