namespace Account.Contracts.Saga.TwoFactor.Events;

public class OtpCodeConfirmedIntegrationEvent
{
    public Guid CorrelationId { get; set; }
    public Guid UserId { get; set; }
    public bool IsValid { get; set; }
}