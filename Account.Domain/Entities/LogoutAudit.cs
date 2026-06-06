using Account.Domain.DTOs.EntitiesDTO;

namespace Account.Domain.Entities;

public class LogoutAudit
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedOutAt { get; set; }

    public static LogoutAudit Create(CreateLogoutAuditDto dto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.UserId, nameof(dto.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.Email, nameof(dto.Email));
        return new LogoutAudit
        {
            UserId = dto.UserId,
            Email = dto.Email,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            LoggedOutAt = dto.LoggedOutAt
        };
    }
}