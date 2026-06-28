using Account.Contracts.Saga.UserRegisterSagaEvents.Models;

namespace Account.Contracts.Saga.UserRegisterSagaEvents.Events;

public class UserRegisterSagaStartedIntegrationEvent : BaseUserModel
{
    public string ? ReferralCode { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
}