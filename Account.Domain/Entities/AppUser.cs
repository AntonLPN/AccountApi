
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Account.Domain.Entities;

public class AppUser
{
    [Key]
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } = "";
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastLogoutAt { get; set; }
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    
    public static AppUser Create(
        string id,
        string email,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("User ID cannot be empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        var user = new AppUser
        {
            Id = id,
            Email = email,
            UserName = email,
            PasswordHash = passwordHash
        };
        //implement here DomainEvent
        return user;
    }
}