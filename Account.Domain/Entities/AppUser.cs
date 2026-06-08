
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    [Comment(
        "Unique code that the user can use to invite others. Automatically generated when the user is created.")]
    public string ReferralCode { get; init; } = ""; //GUID or UUID

    [Comment("ID of the referrer user who invited this user (referrer)")]
    public string? ReferrerId { get; set; } = "";
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    
    public static AppUser Create(
        string id,
        string email,
        string passwordHash,
        string? referrerId)
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
            PasswordHash = passwordHash,
            ReferralCode = GenerateReadableCode(6),
            ReferrerId = referrerId,
            
        };
        return user;
    }
    private static string GenerateReadableCode(int length = 6)
    {
        char[] chars =
            "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }

        return new string(result);
    }
    
}