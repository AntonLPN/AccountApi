namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents.Models;

public class BaseUserModel
{
    public Guid CorrelationId  { get; init; }
    public string UserId  { get; init; } = null!;
    public string Email  { get; init; } = null!;
    public string ApiKey { get; set; } = null!;
}