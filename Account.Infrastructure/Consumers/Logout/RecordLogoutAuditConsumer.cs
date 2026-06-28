using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Logout;

public class RecordLogoutAuditConsumer(
    ILogger<RecordLogoutAuditConsumer> logger,
    ILogoutAuditRepository logoutAuditRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<RecordLogoutAuditIntegrationCommand>
{
    public async Task Consume(ConsumeContext<RecordLogoutAuditIntegrationCommand> context)
    {
        var message = context.Message;
        try
        {
            var logoutAuditDto = new CreateLogoutAuditDto
            {
                UserId = message.UserId,
                Email = message.Email,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                LoggedOutAt = DateTime.UtcNow
            };
            var logoutAudit = LogoutAudit.Create(logoutAuditDto);
            logoutAuditRepository.AddLogout(logoutAudit, context.CancellationToken);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to record logout audit for UserId={UserId}", message.UserId);
            throw;
        }

        logger.LogInformation("Logout audit recorded for UserId={UserId}", message.UserId);

        await context.Publish(new LogoutAuditRecordedIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent
        });
    }
}