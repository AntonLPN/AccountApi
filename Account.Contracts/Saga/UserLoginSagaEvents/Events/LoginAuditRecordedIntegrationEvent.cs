using Account.Contracts.SagaEvents.UserLoginSagaEvents.Models;

namespace Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;

public class LoginAuditRecordedIntegrationEvent : BaseLoginModel
{
    public bool IsSuspicious { get; init; }
}