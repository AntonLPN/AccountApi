using Account.Domain.DTOs;

namespace Account.Domain.Entities;

public class LoginAudit : AggregateRoot
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime LoggedInAt { get; set; }

    public static LoginAudit Create(CreateLoginAuditParams createLoginAuditParams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createLoginAuditParams.UserId, nameof(createLoginAuditParams.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(createLoginAuditParams.Email, nameof(createLoginAuditParams.Email));
        return new LoginAudit
        {
            UserId = createLoginAuditParams.UserId,
            Email = createLoginAuditParams.Email,
            IpAddress = createLoginAuditParams.IpAddress,
            UserAgent = createLoginAuditParams.UserAgent,
            IsSuspicious = createLoginAuditParams.IsSuspicious,
            LoggedInAt = createLoginAuditParams.LoggedInAt
        };
        
    }
}