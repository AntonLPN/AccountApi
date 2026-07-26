using Account.Contracts.Saga.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class UpdateLastLoginConsumer(
    ILogger<UpdateLastLoginConsumer> logger,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<UpdateLastLoginIntegrationCommand>
{
    public async Task Consume(ConsumeContext<UpdateLastLoginIntegrationCommand> context)
    {
        var message = context.Message;
        logger.LogInformation("Updating last login for UserId={UserId}", message.UserId);
        try
        {
            var user = await userRepository.GetUserByEmailAsync(message.Email, context.CancellationToken);
            if (user is null)
            {
                await context.Publish(new UserLoginSagaFailedIntegrationEvent
                {
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    FailureReason = "User not found while updating last login"
                });
                return;
            }
            user.UpdateLastLoginAt();
            await unitOfWork.SaveChangesAsync(context.CancellationToken);  
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update last login for UserId={UserId}", message.UserId);
            throw;
        }
 
    }
}