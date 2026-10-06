using System.Text.Json.Nodes;

namespace Account.Contracts.Saga.UserRegisterSagaEvents.Events;

public class UserRegistrationSagaFailedIntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; } 
    public string?  FailureReason { get; set; }
    public JsonObject? Metadata { get; init; }
}