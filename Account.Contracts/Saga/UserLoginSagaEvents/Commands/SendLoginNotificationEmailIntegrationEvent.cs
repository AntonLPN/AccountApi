using Account.Contracts.SagaEvents.UserLoginSagaEvents.Models;

namespace Account.Contracts.SagaEvents.UserLoginSagaEvents.Commands;

public class SendLoginNotificationEmailIntegrationEvent : BaseLoginModel
{
    public bool IsSuspicious { get; init; }
}