using Account.Contracts.Saga.UserRegisterSagaEvents.Events;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Events;
using Account.Domain.Interfaces;
using Ardalis.SharedKernel;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class UserCreateDomainEventHandler(
    IOutboxEventPublisher publisher,
    IRepository<LoginAudit> loginAuditRepository,
    IRepository<ApiKey> apiKeyRepository) : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await apiKeyRepository.AddAsync(ApiKey.Create(new ApiKeyCreateParams(notification.UserId)), cancellationToken);
        await loginAuditRepository.AddAsync(LoginAudit.Create(new CreateLoginAuditParams
        {
            UserId = notification.UserId,
            Email = notification.Email
        }), cancellationToken);
        
        await publisher.AddOutboxEventAsync(new UserRegisterSagaStartedIntegrationEvent()
        {
            CorrelationId = Guid.NewGuid(),
            UserId = notification.UserId,
            Email = notification.Email
        }, cancellationToken);
    }
}