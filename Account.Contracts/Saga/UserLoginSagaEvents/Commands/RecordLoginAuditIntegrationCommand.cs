using Account.Contracts.SagaEvents.UserLoginSagaEvents.Models;

namespace Account.Contracts.Saga.UserLoginSagaEvents.Commands;

public class RecordLoginAuditIntegrationCommand : BaseLoginModel
{
    public bool IsSuspicious { get; init; }
}