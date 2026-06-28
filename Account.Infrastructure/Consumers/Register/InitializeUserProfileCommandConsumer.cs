using Account.Contracts.Saga.UserRegisterSagaEvents.Commands;
using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Register;

public class InitializeUserProfileCommandConsumer(
    ILogger<InitializeUserProfileCommandConsumer> logger) : IConsumer<InitializeUserProfileIntegrationCommand>
{
    //in the future if case of registration is changed add her logic
    //for today its just step without logic
    public async Task Consume(ConsumeContext<InitializeUserProfileIntegrationCommand> context)
    {
        logger.LogInformation(
            "Starting profile initialization for UserId={UserId}", context.Message.UserId);

        await context.Publish(new UserRegisterProfileInitializedIntegrationEvent
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId
        });
        logger.LogInformation(
            "Profile initialized successfully for UserId={UserId}", context.Message.UserId);
    }
}