using Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;
using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers;

public class CheckSuspiciousLoginConsumer(
    ILogger<CheckSuspiciousLoginConsumer> logger,
    AppDbContext dbContext)
    : IConsumer<CheckSuspiciousLoginIntegrationEvent>
{
    public async Task Consume(ConsumeContext<CheckSuspiciousLoginIntegrationEvent> context)
    {
        var message = context.Message;

        // Simple heuristic: a login is considered suspicious if there is no IP/UserAgent,
        // or if there have been no previous logins from this IP for this user.
        var isSuspicious = string.IsNullOrWhiteSpace(message.IpAddress)
                           || string.IsNullOrWhiteSpace(message.UserAgent);

        if (!isSuspicious)
        {
            var seenIpBefore = await dbContext.LoginAudits
                .AsNoTracking()
                .AnyAsync(a => a.UserId == message.UserId && a.IpAddress == message.IpAddress,
                    context.CancellationToken);

            isSuspicious = !seenIpBefore;
        }

        logger.LogInformation("Suspicious login check for UserId={UserId}: IsSuspicious={IsSuspicious}",
            message.UserId, isSuspicious);

        await context.Publish(new SuspiciousLoginCheckedIntegrationEvent
        {
            CorrelationId = message.CorrelationId,
            UserId = message.UserId,
            Email = message.Email,
            IpAddress = message.IpAddress,
            UserAgent = message.UserAgent,
            IsSuspicious = isSuspicious
        });
    }
}