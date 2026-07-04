namespace Account.Contracts.Saga.TwoFactor.Events;

public class OtpCodeConfirmedIntegrationEvent
{
    public Guid CorrelationId { get; set; }
    public string UserId { get; set; }
    public bool IsValid { get; set; }
}