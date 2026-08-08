using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Domain.Entities;

public class ApiKey : AggregateRoot
{
    [Key] public int Id { get; set; }
    [Column("Key")] public required string ApiKeyValue { get; init; }
    public bool IsAuthorize { get; set; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiredAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    

    public required string UserId { get; init; }
    [ForeignKey(nameof(UserId))] public AppUser AppUser { get; set; }

    public static ApiKey Create(ApiKeyCreateParams createParams)
    {
        var value = Guid.NewGuid().ToString("N");

        return new ApiKey
        {
            ApiKeyValue = value,
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.AddYears(99),
            IsAuthorize = createParams.IsAuthorize,
            UserId = createParams.UserId
        };
    }
    
    public void Revoke()
    {
        IsAuthorize = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

public sealed record ApiKeyCreateParams(string UserId, bool IsAuthorize = true);