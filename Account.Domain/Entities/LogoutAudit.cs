using Account.Domain.DTOs;

namespace Account.Domain.Entities;

public class LogoutAudit : AggregateRoot
{
    public long Id { get; set; }
    public Guid UserId { get; set; } 
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedOutAt { get; set; }

    public static LogoutAudit Create(CreateLogoutCreateParams dto)
    {
        ArgumentNullException.ThrowIfNull(dto.UserId, nameof(dto.UserId));
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