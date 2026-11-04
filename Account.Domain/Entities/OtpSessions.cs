using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Account.Domain.Models;
using Ardalis.GuardClauses;

namespace Account.Domain.Entities;

public class OtpSessions : AggregateRoot
{
    [Key] public int Id { get; set; }
    public required string CodeHash { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? InvalidatedAt { get; set; }

    public required Guid UserId { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public static OtpSessions Create(OtpSessionCreateParams createParams)
    {
        var session = new OtpSessions
        {
            CodeHash = Guard.Against.NullOrWhiteSpace(createParams.CodeHash),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UserId = Guard.Against.Default(createParams.UserId)
        };
        return session;
    }

    public void Invalidate()
    {
        if (UsedAt != null || InvalidatedAt != null) return;
        InvalidatedAt ??= DateTime.UtcNow;
        IsUsed = true;
    }
}