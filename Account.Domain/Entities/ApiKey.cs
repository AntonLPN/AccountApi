using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Domain.Entities;

public class ApiKey : AggregateRoot
{
    [Key] public int Id { get; set; }
    [Column("Key")] public required string HashApiKey { get; init; }
    public required string KeyPrefix { get; set; }
    public bool IsAuthorize { get; set; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiredAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    

    public required string UserId { get; init; }
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }

    public static ApiKey Create(ApiKeyCreateParams createParams)
    {
        return new ApiKey
        {
            HashApiKey = createParams.HashApiKey,
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.AddYears(99),
            IsAuthorize = createParams.IsAuthorize,
            UserId = createParams.UserId,
            KeyPrefix = createParams.ApiKey.Substring(0, 8)
        };
    }
    
    public void Revoke()
    {
        IsAuthorize = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

public sealed record ApiKeyCreateParams(string UserId,string ApiKey, string HashApiKey, bool IsAuthorize = true);