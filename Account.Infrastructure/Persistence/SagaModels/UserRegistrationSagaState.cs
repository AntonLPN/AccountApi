using MassTransit;

namespace Account.Infrastructure.Persistence.SagaModels;

public class UserRegistrationSagaState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int Version { get; set; }

    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";

    public string ApiKey { get; set; } = "";
    public bool EmailConfirmationSent { get; set; }
    public bool ProfileInitialized { get; set; }
    public string FailureReason { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}