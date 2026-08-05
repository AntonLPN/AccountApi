using MediatR;

namespace Account.Domain.Events;

public class TwoFactorInitiatedDomainEvent : INotification
{
    public Guid CorrelationId { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; } = "";
    public string OtpCode { get; set; }
    public DateTime ExpirationTime { get; set; }
}