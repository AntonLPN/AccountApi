namespace Account.Contracts.Saga.UserRegisterSagaEvents.Events;

public class UserRegistrationSagaFailedIntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string?  FailureReason { get; set; }
}