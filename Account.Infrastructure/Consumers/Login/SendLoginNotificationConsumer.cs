using Account.Contracts.Saga.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class SendLoginNotificationConsumer(
    ILogger<SendLoginNotificationConsumer> logger,
    IEmail emailService)
    : IConsumer<SendLoginNotificationEmailIntegrationCommand>
{
    public async Task Consume(ConsumeContext<SendLoginNotificationEmailIntegrationCommand> context)
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
                await context.Publish(new UserLoginSagaFailedIntegrationEvent
                {
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    FailureReason = "Failed to send login notification email"
                });
                return;
            }

            logger.LogInformation("Login notification sent for UserId={UserId}, Email={Email}",
                message.UserId, MaskedEmail.Create(message.Email));
        }

        await context.Publish(new LoginNotificationSentIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent
        });
    }
}