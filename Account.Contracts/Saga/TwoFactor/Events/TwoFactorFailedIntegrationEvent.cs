namespace Account.Contracts.Saga.TwoFactor.Events;

public class TwoFactorFailedIntegrationEvent : BaseTwoFactorModel
{
    public string?  FailureReason { get; set; }
}