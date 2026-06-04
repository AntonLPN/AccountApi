namespace Account.Contracts.SagaEvents.UserLoginSagaEvents.Models;

public class BaseLoginModel
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}