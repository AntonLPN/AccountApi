using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Account.Contracts.Events.External;

//This event needs it for sending to external system in microservice architecture,
//for example, secondary api
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class UserRegisteredIntegrationEvent
{
    public Guid CorrelationId { get; set; }
    public Guid? UserId { get; init; }
    public string Email { get; init; } = "";
    public string? PhoneNumber { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    
    public JsonObject? Metadata { get; init; }
}