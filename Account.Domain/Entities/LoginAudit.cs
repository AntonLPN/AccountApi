using Account.Domain.DTOs.EntitiesDTO;

namespace Account.Domain.Entities;

public class LoginAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime LoggedInAt { get; set; }

    public static LoginAudit Create(CreateLoginAuditDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.UserId, nameof(dto.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Email, nameof(dto.Email));
        return new LoginAudit
        {
            UserId = dto.UserId,
            Email = dto.Email,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            IsSuspicious = dto.IsSuspicious,
            LoggedInAt = dto.LoggedInAt
        };
        
    }
}