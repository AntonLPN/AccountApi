using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class SendLoginNotificationConsumer(
    ILogger<SendLoginNotificationConsumer> logger,
    IEmail emailService)
    : IConsumer<SendLoginNotificationEmailIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SendLoginNotificationEmailIntegrationEvent> context)
    {
        var message = context.Message;

        var template = message.IsSuspicious ? "SuspiciousLoginTemplate.html" : "LoginNotificationTemplate.html";
        var sent = await emailService.SendEmail(message.Email, template, context.CancellationToken);

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