namespace Account.Contracts.UserLogin.Models;

public class BaseLoginModel
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; } 
    public string Email { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}