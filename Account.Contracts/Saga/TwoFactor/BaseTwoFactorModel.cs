namespace Account.Contracts.Saga.TwoFactor;

public class BaseTwoFactorModel
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; } 
    public string Email { get; init; } = null!;
    public string OtpCode { get; set; } = null!;
}