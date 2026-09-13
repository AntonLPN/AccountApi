using Account.Domain.DTOs;
using Account.Domain.Entities;
using Account.Domain.Events;
using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Account.Application.DomainEventsHandlers;

public class UserLoggedOutDomainEventHandler(
    ILogger<UserLoggedOutDomainEventHandler> logger,
    IRepository<LogoutAudit> logoutAuditRepository)
    : INotificationHandler<UserLoggedOutDomainEvent>
{
    public async Task Handle(UserLoggedOutDomainEvent notification, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred while handling UserLoggedOutDomainEvent");
            throw;
        }
    }
}