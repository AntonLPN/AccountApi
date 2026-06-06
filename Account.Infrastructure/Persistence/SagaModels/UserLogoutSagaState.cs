using MassTransit;

namespace Account.Infrastructure.Persistence.SagaModels;

public class UserLogoutSagaState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public int Version { get; set; }

    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool AuditRecorded { get; set; }
    public bool LastLogoutUpdated { get; set; }
    public bool NotificationSent { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}