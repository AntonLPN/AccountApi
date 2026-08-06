using Account.Domain.Events;
using Account.Domain.Interfaces;
using MediatR;

namespace Account.Application.DomainEventsHandlers;

public class UserCreateEventHandler(IOutboxEventPublisher publisher) : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        
        
        //TODO implement here the logic for the Domain event
        Console.WriteLine("Domain Event: UserCreatedDomainEvent logic not implemented");
    }
}