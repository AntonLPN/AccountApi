using Account.Contracts.Events;
using Account.Domain.Entities;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.ChangePassword;

public class ChangePasswordConsumer(
    ILogger<ChangePasswordConsumer> logger,
    IRepository<AppUser> userRepository,
    IEmail emailSender) : IConsumer<ChangePasswordIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ChangePasswordIntegrationEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Change Password Event Received");
        try
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByIdSpec(message.UserId), context.CancellationToken);
            if (user is null)
            {
                logger.LogError("User not found in the database for id:  {UserId} in ChangePasswordConsumer",
                    message.UserId);
                throw new Exception(
                    $"User not found in the database for id:  {message.UserId} in ChangePasswordConsumer");
            }


            await emailSender.SendPasswordChangedEmailAsync(user.Email, context.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send password changed email to user with id: {UserId}",
                message.UserId);
            throw;
        }
    }
}