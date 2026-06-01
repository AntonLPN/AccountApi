using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class InitializeUserProfileCommandConsumer(
    ILogger<InitializeUserProfileCommandConsumer> logger) : IConsumer<InitializeUserProfileCommandIntegrationEvent>
{
    //in the future if case of registration is changed add her logic
    //for today its just step without logic
    public async Task Consume(ConsumeContext<InitializeUserProfileCommandIntegrationEvent> context)
    {
        logger.LogInformation(
            "Starting profile initialization for UserId={UserId}", context.Message.UserId);

        await context.Publish(new UserProfileInitializedIntegrationEvent
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId
        });
        logger.LogInformation(
            "Profile initialized successfully for UserId={UserId}", context.Message.UserId);
    }
}