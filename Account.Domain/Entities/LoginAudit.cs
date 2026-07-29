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

    public static LoginAudit Create(CreateLoginAuditParams @params)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(@params.UserId, nameof(@params.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(@params.Email, nameof(@params.Email));
        return new LoginAudit
        {
            UserId = @params.UserId,
            Email = @params.Email,
            IpAddress = @params.IpAddress,
            UserAgent = @params.UserAgent,
            IsSuspicious = @params.IsSuspicious,
            LoggedInAt = @params.LoggedInAt
        };
    }
}