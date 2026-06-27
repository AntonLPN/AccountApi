namespace Account.Contracts.Saga.TwoFactor.Events;

public class TwoFactorSagaStartedIntegrationEvent : BaseTwoFactorModel
{
    public DateTime ExpirationTime { get; set; }
}