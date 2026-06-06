namespace Account.Contracts.SagaEvents.UserLogoutSagaEvents.Events;

public class UserLogoutSagaFailedIntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string? FailureReason { get; set; }
}