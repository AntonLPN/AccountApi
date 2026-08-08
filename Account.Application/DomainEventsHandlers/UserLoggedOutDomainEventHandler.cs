using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Events;
using Ardalis.SharedKernel;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class UserLoggedOutDomainEventHandler(IRepository<LogoutAudit> logoutAuditRepository)
    : INotificationHandler<UserLoggedOutDomainEvent>
{
    public async Task Handle(UserLoggedOutDomainEvent notification, CancellationToken cancellationToken)
    {
        var logoutAuditDto = new CreateLogoutCreateParams
        {
            UserId = notification.UserId,
            Email = notification.Email,
            IpAddress = notification.IpAddress,
            UserAgent = notification.UserAgent,
            LoggedOutAt = DateTime.UtcNow
        };
        var logoutAudit = LogoutAudit.Create(logoutAuditDto);
        await logoutAuditRepository.AddAsync(logoutAudit, cancellationToken);
        
    }
}