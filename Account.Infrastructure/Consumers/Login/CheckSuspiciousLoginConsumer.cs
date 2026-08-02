using Account.Contracts.Saga.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Entities;
using Account.Domain.Specifications;
using Ardalis.SharedKernel;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.Login;

public class CheckSuspiciousLoginConsumer(
    ILogger<CheckSuspiciousLoginConsumer> logger,
    IRepository<LoginAudit> loginAuditRepository)
    : IConsumer<CheckSuspiciousLoginIntegrationCommand>
{
    public async Task Consume(ConsumeContext<CheckSuspiciousLoginIntegrationCommand> context)
    {
        var message = context.Message;
        ArgumentException.ThrowIfNullOrEmpty(message.UserId, nameof(message.UserId));
        ArgumentException.ThrowIfNullOrEmpty(message.UserAgent, nameof(message.UserAgent));
        ArgumentException.ThrowIfNullOrEmpty(message.IpAddress, nameof(message.IpAddress));

        var seenDeviceBefore =
            await loginAuditRepository.AnyAsync(
                new LoginAuditByUserAndUserAgentAsReadOnlySpec(message.UserId, message.UserAgent),
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