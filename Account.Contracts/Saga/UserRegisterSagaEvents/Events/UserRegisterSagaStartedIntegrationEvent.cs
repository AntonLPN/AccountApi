using Account.Contracts.Saga.UserRegisterSagaEvents.Models;

namespace Account.Contracts.Saga.UserRegisterSagaEvents.Events;

public class UserRegisterSagaStartedIntegrationEvent : BaseUserModel
{
    public bool IsEmailConfirmed { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
}