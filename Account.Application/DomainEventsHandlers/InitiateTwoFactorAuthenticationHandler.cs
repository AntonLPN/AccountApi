using Account.Contracts.Saga.TwoFactor.Events;
using Account.Domain.Events;
using Account.Domain.Interfaces;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class InitiateTwoFactorAuthenticationHandler(IOutboxEventPublisher publisher) : INotificationHandler<TwoFactorInitiatedDomainEvent>
{
    public async Task Handle(TwoFactorInitiatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new TwoFactorSagaStartedIntegrationEvent
        {
            CorrelationId = notification.CorrelationId,
            UserId = notification.UserId,
            Email = notification.Email,
            OtpCode = notification.OtpCode,
            ExpirationTime = notification.ExpirationTime
        };

        await publisher.AddOutboxEventAsync(integrationEvent, cancellationToken);
    }
}