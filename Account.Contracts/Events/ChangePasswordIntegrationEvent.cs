namespace Account.Contracts.Events;

public class ChangePasswordIntegrationEvent
{
    public required Guid CorrelationId { get; set; }

    public required string UserId { get; set; }
}