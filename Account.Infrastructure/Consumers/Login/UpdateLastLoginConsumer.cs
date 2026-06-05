using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class UpdateLastLoginConsumer(
    ILogger<UpdateLastLoginConsumer> logger,
    IUserRepository userRepository)
    : IConsumer<UpdateLastLoginIntegrationEvent>
{
    public async Task Consume(ConsumeContext<UpdateLastLoginIntegrationEvent> context)
    {
        var message = context.Message;
        
        var updated = await userRepository.UpdateLastLoginAsync(message.UserId, DateTime.UtcNow,
            context.CancellationToken);

        if (!updated)
        {
            await context.Publish(new UserLoginSagaFailedIntegrationEvent
            {
                CorrelationId = message.CorrelationId,
                UserId = message.UserId,
                FailureReason = "User not found while updating last login"
            });
            return;
        }

        logger.LogInformation("Last login updated for UserId={UserId}", message.UserId);

        await context.Publish(new LastLoginUpdatedIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent
        });
    }
}