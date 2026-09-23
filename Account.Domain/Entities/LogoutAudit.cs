using System.ComponentModel.DataAnnotations.Schema;
using Account.Domain.DTOs;
using Ardalis.GuardClauses;

namespace Account.Domain.Entities;

public class LogoutAudit : AggregateRoot
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedOutAt { get; set; }
    
    public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }

    public static LogoutAudit Create(CreateLogoutCreateParams dto)
    {
        return new LogoutAudit
        {
            UserId = Guard.Against.Default(dto.UserId),
            Email = dto.Email,
            IpAddress = dto.IpAddress,
            UserAgent = dto.UserAgent,
            LoggedOutAt = Guard.Against.Default(dto.LoggedOutAt)
        };
    }
}