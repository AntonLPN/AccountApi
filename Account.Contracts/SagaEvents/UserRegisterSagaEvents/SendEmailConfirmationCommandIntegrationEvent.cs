namespace Account.Contracts.SagaEvents.UserRegisterSagaEvents;

public class SendEmailConfirmationCommandIntegrationEvent
{
    public Guid CorrelationId  { get; init; }
    public string UserId  { get; init; } = null!;
    public string Email  { get; init; } = null!;
}