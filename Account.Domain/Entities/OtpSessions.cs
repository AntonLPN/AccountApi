using System.ComponentModel.DataAnnotations;
using Account.Domain.Models;

namespace Account.Domain.Entities;

public class OtpSessions
{
    [Key] public int Id { get; set; }
    public required string CodeHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; } = null;
    public string UserId { get; set; } = "";
    public AppUser AppUser { get; set; }

    public static OtpSessions Create(OtpSessionCreateParams createParams)
    {
        
        var session = new OtpSessions
        {
            CodeHash =  createParams.CodeHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UserId = createParams.UserId
        };
        return session;
        
    }
}