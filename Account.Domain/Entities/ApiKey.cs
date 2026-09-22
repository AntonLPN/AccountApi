using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ardalis.GuardClauses;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Account.Domain.Entities;

public class ApiKey : AggregateRoot
{
    [Key] public int Id { get; set; }
    [Column("Key")] public required string HashApiKey { get; init; }
    public required string KeyPrefix { get; init; }
    public bool IsAuthorize { get; set; } = true; 
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiredAt { get; init; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    

    public required Guid UserId { get; init; }
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }

    public static ApiKey Create(ApiKeyCreateParams createParams)
    {
        Guard.Against.NullOrWhiteSpace(createParams.ApiKey, nameof(createParams.ApiKey));
        
        return new ApiKey
        {
            HashApiKey = Guard.Against.NullOrWhiteSpace(createParams.HashApiKey),
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.AddYears(99),
            IsAuthorize = createParams.IsAuthorize,
            UserId = Guard.Against.Default(createParams.UserId),
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

public sealed record ApiKeyCreateParams(Guid UserId,string ApiKey, string HashApiKey, bool IsAuthorize = true);