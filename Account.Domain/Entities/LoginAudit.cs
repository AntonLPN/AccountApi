using System.ComponentModel.DataAnnotations;
using Account.Domain.DTOs;
using Ardalis.GuardClauses;

namespace Account.Domain.Entities;

public class LoginAudit : AggregateRoot
{
    [Key] public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuspicious { get; set; }
    public DateTime LoggedInAt { get; set; }

    public static LoginAudit Create(CreateLoginAuditParams createLoginAuditParams)
    {
        return new LoginAudit
        {
            UserId = Guard.Against.Default(createLoginAuditParams.UserId),
            Email = Guard.Against.NullOrWhiteSpace(createLoginAuditParams.Email),
            IpAddress = createLoginAuditParams.IpAddress,
            UserAgent = createLoginAuditParams.UserAgent,
            IsSuspicious = createLoginAuditParams.IsSuspicious,
            LoggedInAt = Guard.Against.Default(createLoginAuditParams.LoggedInAt)
        };
    }
}