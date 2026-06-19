using Account.Contracts.SagaEvents.UserRegisterSagaEvents.Models;

namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;

public class UserSagaStartedIntegrationEvent : BaseUserModel
{
    public string ? ReferralCode { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
}