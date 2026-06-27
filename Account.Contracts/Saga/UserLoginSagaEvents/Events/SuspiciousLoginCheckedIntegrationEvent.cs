using Account.Contracts.SagaEvents.UserLoginSagaEvents.Models;

namespace Account.Contracts.SagaEvents.UserLoginSagaEvents.Events;

public class SuspiciousLoginCheckedIntegrationEvent : BaseLoginModel
{
    public bool IsSuspicious { get; init; }
}