using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Logout;

public class SendLogoutNotificationConsumer(
    ILogger<SendLogoutNotificationConsumer> logger,
    IEmail emailService)
    : IConsumer<SendLogoutNotificationEmailIntegrationCommand>
{
    public async Task Consume(ConsumeContext<SendLogoutNotificationEmailIntegrationCommand> context)
    {
        var message = context.Message;

        logger.LogInformation("Sending logout notification for UserId={UserId}, Email={Email}",
            message.UserId, MaskedEmail.Create(message.Email));

        var logoutNotificationDto = new LogoutNotification(
            message.Email,
            message.IpAddress,
            DateTime.UtcNow,
            message.UserAgent ?? "Unknown device"
        );

        var sent = await emailService.SendLogoutNotificationEmail(logoutNotificationDto, context.CancellationToken);
        if (!sent)
        {
            await context.Publish(new UserLogoutSagaFailedIntegrationEvent
            {
                CorrelationId = message.CorrelationId,
                UserId = message.UserId,
                FailureReason = "Failed to send logout notification email"
            });
            return;
        }

        logger.LogInformation("Logout notification sent for UserId={UserId}, Email={Email}",
            message.UserId, MaskedEmail.Create(message.Email));

        await context.Publish(new LogoutNotificationSentIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent
        });
    }
}