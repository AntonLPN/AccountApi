using Account.Contracts.UserLogin;
using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Events;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Domain.Specifications;
using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Account.Application.DomainEventsHandlers;

public class UserLoggedInDomainEventHandler(ILogger<UserLoggedInDomainEventHandler> logger,
    IRepository<LoginAudit> loginAuditRepository,
    IOutboxEventPublisher publisher,
    IUnitOfWork unitOfWork) : INotificationHandler<UserLoggedInDomainEvent>
{
    public async Task Handle(UserLoggedInDomainEvent notification, CancellationToken cancellationToken)
    {
        if (notification.UserAgent == null)
        {
            logger.LogWarning("User logged in without user agent");
            return;
        }
        var seenDeviceBefore =
            await loginAuditRepository.AnyAsync(
                new LoginAuditByUserAndUserAgentAsReadOnlySpec(notification.UserId, notification.UserAgent),
                cancellationToken);
        if (!seenDeviceBefore)
        {
            var loginAuditDto = new CreateLoginAuditParams
            {
                UserId = notification.UserId,
                Email = notification.Email,
                IpAddress = notification.IpAddress,
                UserAgent = notification.UserAgent,
                IsSuspicious = true, 
                LoggedInAt = DateTime.UtcNow
            };
            var loginAudit = LoginAudit.Create(loginAuditDto);
            await loginAuditRepository.AddAsync(loginAudit, cancellationToken);
            
            await publisher.AddOutboxEventAsync(new SendLoginNotificationEmailIntegrationEvent
            {
                CorrelationId = Guid.NewGuid(),
                UserId = notification.UserId,
                Email = notification.Email,
                IpAddress = notification.IpAddress,
                UserAgent = notification.UserAgent,
                IsSuspicious = true
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

    }
}