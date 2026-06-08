namespace Account.Contracts.Events.RegisterEvents;

//This event needs it for sending to external system in microservice architecture,
//for example, secondary api
public class UserRegisteredIntegrationEvent
{
    public Guid CorrelationId { get; set; }
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? ApiKey { get; init; }
    public string? ReferralId { get; set; }
}