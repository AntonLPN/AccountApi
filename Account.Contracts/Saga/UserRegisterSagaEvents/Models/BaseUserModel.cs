namespace Account.Contracts.Saga.UserRegisterSagaEvents.Models;

public class BaseUserModel
{
    public Guid CorrelationId  { get; init; }
    public Guid UserId  { get; init; } 
    public string Email  { get; init; } = null!;
}