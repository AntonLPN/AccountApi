namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents.Events;

public class EmailConfirmationSentIntegrationEvent
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string ApiKey { get; set; } = null!;
}