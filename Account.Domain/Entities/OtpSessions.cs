using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Account.Domain.Models;
using Ardalis.Result;

namespace Account.Domain.Entities;

public class OtpSessions : AggregateRoot
{
    [Key] public int Id { get; set; }
    public required Guid CorrelationId { get; set; }
    public required string CodeHash { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? InvalidatedAt { get; set; }
    public required string UserId { get; set; }
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }

    public static OtpSessions Create(OtpSessionCreateParams createParams)
    {
        var session = new OtpSessions
        {
            CodeHash = createParams.CodeHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UserId = createParams.UserId,
            CorrelationId = createParams.CorrelationId
        };
        return session;
    }

    public Result Validate(DateTime utcNow)
    {
        if (UsedAt!=null || InvalidatedAt != null)
            return Result.NotFound("No active OTP session found for the user or OTP already used");

        if (ExpiresAt < utcNow)
            return Result.Conflict("OTP session expired");

        return Result.Success();
    }
    public void Invalidate()
    {
        if (UsedAt == null && InvalidatedAt == null)
            InvalidatedAt ??= DateTime.UtcNow;
    }
}