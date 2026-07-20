using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Account.Domain.Models;

namespace Account.Domain.Entities;

public class OtpSessions
{
    [Key] public int Id { get; set; }
    public required Guid CorrelationId { get; set; }
    public required string CodeHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; } = null;
    public DateTime? InvalidatedAt { get; set; }
    public required string UserId { get; set;}
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
}