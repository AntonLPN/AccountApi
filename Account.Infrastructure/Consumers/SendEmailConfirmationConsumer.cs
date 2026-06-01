using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class SendEmailConfirmationConsumer(ILogger<SendEmailConfirmationConsumer> logger)
    : IConsumer<SendEmailConfirmationCommandIntegrationEvent>
{
    public Task Consume(ConsumeContext<SendEmailConfirmationCommandIntegrationEvent> context)
    {
        //TODO implement logic for sending email confirmation, for now just simulating with log and publish next event in saga
        logger.LogInformation(
            "Consumed SendEmailConfirmationCommandIntegrationEvent: UserId={UserId}, Email={Email}",
            context.Message.UserId, context.Message.Email);
        return context.Publish(new EmailConfirmationSentIntegrationEvent
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId
        });
    }
}