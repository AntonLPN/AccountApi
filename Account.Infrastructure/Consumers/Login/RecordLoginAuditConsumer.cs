using Account.Contracts.Saga.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class RecordLoginAuditConsumer(
    ILogger<RecordLoginAuditConsumer> logger,
    IRepository<LoginAudit> loginAuditRepository)
    : IConsumer<RecordLoginAuditIntegrationCommand>
{
    public async Task Consume(ConsumeContext<RecordLoginAuditIntegrationCommand> context)
    {
        var message = context.Message;
        try
        {
            var loginAuditDto = new CreateLoginAuditParams
            {
                UserId = message.UserId,
                Email = message.Email,
                IpAddress = message.IpAddress,
                UserAgent = message.UserAgent,
                IsSuspicious = message.IsSuspicious, 
                LoggedInAt = DateTime.UtcNow
            };
            var loginAudit = LoginAudit.Create(loginAuditDto);
            await loginAuditRepository.AddAsync(loginAudit, context.CancellationToken);
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