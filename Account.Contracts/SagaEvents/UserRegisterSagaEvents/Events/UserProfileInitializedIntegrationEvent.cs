using System.Security.AccessControl;

namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;

public class UserProfileInitializedIntegrationEvent
{
    public Guid CorrelationId  { get; init; }
    public string UserId  { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
}