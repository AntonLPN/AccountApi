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
    IRepository<LoginAudit> loginAuditRepository) : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
    
        await loginAuditRepository.AddAsync(LoginAudit.Create(new CreateLoginAuditParams
        {
            UserId = notification.UserId,
            Email = notification.Email,
            IpAddress = notification.IpAddress,
            UserAgent = notification.UserAgent,
            
        }), cancellationToken);

        await publisher.AddOutboxEventAsync(new UserRegisterSagaStartedIntegrationEvent()
        {
            CorrelationId = Guid.NewGuid(),
            UserId = notification.UserId,
            Email = notification.Email
        }, cancellationToken);
    }
}