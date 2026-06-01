namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents.Commands;

public class InitializeUserProfileCommandIntegrationEvent
{
    public Guid CorrelationId  { get; init; }
    public string UserId  { get; init; } = null!;
    public string Email  { get; init; } = null!;
    public string ApiKey { get; set; } = null!;
}