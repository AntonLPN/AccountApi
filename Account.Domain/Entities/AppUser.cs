
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
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    
    public static AppUser Create(
        string id,
        string email,
        string passwordHash)
    {
        var user = new AppUser
        {
            Id = id,
            Email = email,
            UserName = email,
            PasswordHash = passwordHash
        };
        //TODO implement here DomainEvent
        return user;
    }
}