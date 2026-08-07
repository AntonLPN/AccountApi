using Account.Contracts.Events;
using Account.Domain.Events;
using Account.Domain.Interfaces;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class PasswordChangedDomainEventHandler(IOutboxEventPublisher publisher) : INotificationHandler<PasswordChangedDomainEvent>
{
    public async Task Handle(PasswordChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await publisher.AddOutboxEventAsync(new ChangePasswordIntegrationEvent
        {
            CorrelationId = Guid.NewGuid(),
            UserId = notification.UserId
        }, cancellationToken);
    }
}