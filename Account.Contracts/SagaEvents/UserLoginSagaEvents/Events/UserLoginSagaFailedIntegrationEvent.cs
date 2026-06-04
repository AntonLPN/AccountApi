namespace Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;

public class UserLoginSagaFailedIntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string? FailureReason { get; set; }
}