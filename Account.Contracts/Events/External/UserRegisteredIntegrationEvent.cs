using System.Diagnostics.CodeAnalysis;

namespace Account.Contracts.Events.External;

//This event needs it for sending to external system in microservice architecture,
//for example, secondary api
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class UserRegisteredIntegrationEvent
{
    public Guid CorrelationId { get; set; }
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public string? ApiKey { get; init; }
    public string? ReferralCode { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
}