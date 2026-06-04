using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class SendEmailConfirmationConsumer(ILogger<SendEmailConfirmationConsumer> logger,IEmail emailService)
    : IConsumer<SendEmailConfirmationIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SendEmailConfirmationIntegrationEvent> context)
    {
        var res = await emailService.SendEmail(context.Message.Email, "WelcomeTemplate.html");
        if (!res)
        {
            await context.Publish(new UserRegistrationSagaFailedIntegrationEvent
            {
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                FailureReason = "Failed to send welcome email"
            });
            return;
        }
        logger.LogInformation(
            "Consumed SendEmailConfirmationCommandIntegrationEvent: UserId={UserId}, Email={Email}",
            context.Message.UserId, MaskedEmail.Create(context.Message.Email));
        await context.Publish(new EmailConfirmationSentIntegrationEvent
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId
        });
    }
}