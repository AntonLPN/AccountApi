using System.Text.Json.Nodes;

namespace Account.Contracts.Saga.UserRegisterSagaEvents.Models;

public class BaseUserModel
{
    public Guid CorrelationId  { get; init; }
    public Guid UserId  { get; init; } 
    public string Email  { get; init; } = null!;
    public JsonObject? Metadata { get; init; }
}