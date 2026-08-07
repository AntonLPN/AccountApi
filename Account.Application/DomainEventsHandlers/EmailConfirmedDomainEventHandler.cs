using Account.Domain.Events;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class EmailConfirmedDomainEventHandler:INotificationHandler<EmailConfirmedDomainEvent>
{
    public Task Handle(EmailConfirmedDomainEvent notification, CancellationToken cancellationToken)
    {
        //TODO implement here the logic for the Domain event
        Console.WriteLine("Domain Event: EmailConfirmationDomainEvent logic not implemented");
        return Task.CompletedTask;
    }
}