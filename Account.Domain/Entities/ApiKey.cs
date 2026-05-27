using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Domain.Entities;

public class ApiKey
{
    [Key] public int Id { get; set; }
    [Column("Key")] public string ApiKeyValue { get; set; } = "";
    public bool IsAuthorize { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public string? UserId { get; set; } 
    [ForeignKey(nameof(UserId))] public AppUser? AppUser { get; set; } = new();
}