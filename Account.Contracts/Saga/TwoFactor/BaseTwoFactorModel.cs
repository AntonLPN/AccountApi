namespace Account.Contracts.Saga.TwoFactor;

public class BaseTwoFactorModel
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string OtpCode { get; set; } = null!;
}