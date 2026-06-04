using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class RecordLoginAuditConsumer(
    ILogger<RecordLoginAuditConsumer> logger,
    AppDbContext dbContext)
    : IConsumer<RecordLoginAuditIntegrationEvent>
{
    public async Task Consume(ConsumeContext<RecordLoginAuditIntegrationEvent> context)
    {
        var message = context.Message;
        try
        {
            dbContext.LoginAudits.Add(new LoginAudit
            {
                UserId = message.UserId,
                Email = message.Email,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                IsSuspicious = message.IsSuspicious,
                LoggedInAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to record login audit for UserId={UserId}", message.UserId);
            throw;
        }


        logger.LogInformation("Login audit recorded for UserId={UserId}", message.UserId);

        await context.Publish(new LoginAuditRecordedIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            IsSuspicious = message.IsSuspicious
        });
    }
}