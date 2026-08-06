using Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;
using Account.Domain.Events;
using Account.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Account.Application.DomainEventsHandlers;

public class UserLoggedInEventHandler(ILogger<UserLoggedInEventHandler> logger, IOutboxEventPublisher publisher) : INotificationHandler<UserLoggedInDomainEvent>
{
    public async Task Handle(UserLoggedInDomainEvent notification, CancellationToken cancellationToken)
    {
        await publisher.AddOutboxEventAsync(new UserLoginSagaStartedIntegrationEvent
        {
            CorrelationId = Guid.NewGuid(),
            UserId = notification.UserId,
            Email = notification.Email,
            IpAddress = notification.IpAddress,
            UserAgent = notification.UserAgent
        }, cancellationToken);
    }
}