
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Account.Domain.Entities;

public class AppUser
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}