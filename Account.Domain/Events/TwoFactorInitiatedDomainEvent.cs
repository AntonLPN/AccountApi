using MediatR;

namespace Account.Domain.Events;

public class TwoFactorInitiatedDomainEvent : INotification
{
    public Guid CorrelationId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public required string OtpCode { get; set; }
    public DateTime ExpirationTime { get; set; }
}