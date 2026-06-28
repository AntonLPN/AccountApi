using Account.Contracts.Saga.UserRegisterSagaEvents.Commands;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using Account.Domain.Interfaces;
using Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Register;

public class SendWelcomeEmailConsumer(ILogger<SendWelcomeEmailConsumer> logger, IEmail emailService)
    : IConsumer<SendWelcomeEmailIntegrationCommand>
{
    public async Task Consume(ConsumeContext<SendWelcomeEmailIntegrationCommand> context)
    {
        var res = await emailService.SendWelcomeEmail(context.Message.Email, context.CancellationToken);
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
            "Consumed SendWelcomeEmailIntegrationEvent: UserId={UserId}, Email={Email}",
            context.Message.UserId, MaskedEmail.Create(context.Message.Email));
        await context.Publish(new WelcomeEmailSentIntegrationEvent
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId
        });
    }
}