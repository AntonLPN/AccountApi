using MassTransit;

namespace Account.Infrastructure.Persistence.SagaModels;

public class TwoFactorSagaState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
    public string CurrentState { get; set; } = null!;
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string OtpCode { get; set; }
    public bool OtpCodeSent { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ExpiredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}