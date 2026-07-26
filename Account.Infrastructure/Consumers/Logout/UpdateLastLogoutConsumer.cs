using Account.Contracts.Saga.UserLogoutSagaEvents.Commands;
using Account.Contracts.Saga.UserLogoutSagaEvents.Events;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Logout;

public class UpdateLastLogoutConsumer(
    ILogger<UpdateLastLogoutConsumer> logger,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<UpdateLastLogoutIntegrationCommand>
{
    public async Task Consume(ConsumeContext<UpdateLastLogoutIntegrationCommand> context)
    {
        var message = context.Message;
        logger.LogInformation("Updating last logout for UserId={UserId}", message.UserId);
        try
        {
            var user = await userRepository.GetUserByEmailAsync(message.Email, context.CancellationToken);
            if (user is null)
            {
                await context.Publish(new UserLogoutSagaFailedIntegrationEvent
                {
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    FailureReason = "User not found while updating last logout"
                });
                return;
            }

            user.UpdateLastLogoutAt();
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Last logout updated for UserId={UserId}", message.UserId);

            await context.Publish(new LastLogoutUpdatedIntegrationEvent
            {
                CorrelationId = message.CorrelationId,
                UserId = message.UserId,
                Email = message.Email,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update last logout for UserId={UserId}", message.UserId);
            throw;
        }
    }
}