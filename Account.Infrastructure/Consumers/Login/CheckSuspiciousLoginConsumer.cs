using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class CheckSuspiciousLoginConsumer(
    ILogger<CheckSuspiciousLoginConsumer> logger,
    ILoginAuditRepository loginAuditRepository)
    : IConsumer<CheckSuspiciousLoginIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CheckSuspiciousLoginIntegrationEvent> context)
    {
        var message = context.Message;
        ArgumentException.ThrowIfNullOrEmpty(message.UserId, nameof(message.UserId));
        ArgumentException.ThrowIfNullOrEmpty(message.UserAgent, nameof(message.UserAgent));
        ArgumentException.ThrowIfNullOrEmpty(message.IpAddress, nameof(message.IpAddress));
        
        var seenDeviceBefore =
            await loginAuditRepository.IsNewDeviceLoginAsync(message.UserId, message.UserAgent,
                context.CancellationToken);

        logger.LogInformation("Suspicious login check for UserId={UserId}: IsSuspicious={IsSuspicious}",
            message.UserId, seenDeviceBefore);

        await context.Publish(new SuspiciousLoginCheckedIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            IsSuspicious = !seenDeviceBefore
        });
    }
}