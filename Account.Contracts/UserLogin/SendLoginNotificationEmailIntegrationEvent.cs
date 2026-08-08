using Account.Contracts.UserLogin.Models;

namespace Account.Contracts.UserLogin;

public class SendLoginNotificationEmailIntegrationEvent : BaseLoginModel
{
    public bool IsSuspicious { get; init; }
}