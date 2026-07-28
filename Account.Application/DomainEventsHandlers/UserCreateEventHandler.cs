using Account.Domain.Events;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class UserCreateEventHandler : INotificationHandler<UserCreatedDomainEvent>
{
    public Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        //TODO implement here the logic for the Domain event
        Console.WriteLine("Domain Event: UserCreatedDomainEvent logic not implemented");
        return Task.CompletedTask;
    }
}