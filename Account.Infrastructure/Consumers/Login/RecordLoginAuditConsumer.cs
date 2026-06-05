using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.DTOs.EntitiesDTO;
using Account.Domain.Entities;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class RecordLoginAuditConsumer(
    ILogger<RecordLoginAuditConsumer> logger,
    ILoginAuditRepository loginAuditRepository,
    IUnitOfWork unitOfWork)
    : IConsumer<RecordLoginAuditIntegrationEvent>
{
    public async Task Consume(ConsumeContext<RecordLoginAuditIntegrationEvent> context)
    {
        var message = context.Message;
        try
        {
            var loginAuditDto = new CreateLoginAuditDto
            {
                UserId = message.UserId,
                Email = message.Email,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                IsSuspicious = message.IsSuspicious, 
                LoggedInAt = DateTime.UtcNow
            };
            var loginAudit = LoginAudit.Create(loginAuditDto);
            loginAuditRepository.AddLogin(loginAudit, context.CancellationToken);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
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