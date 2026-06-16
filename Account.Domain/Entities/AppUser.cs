using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Account.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Account.Domain.Entities;

public class AppUser
{
    [Key] public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; } = "";
    
    public string? ProviderName { get; set; } = "my-corporate-ad";//Google, Aple, etc.
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastLogoutAt { get; set; }

    [Comment(
        "Unique code that the user can use to invite others. Automatically generated when the user is created.")]
    public string ReferralCode { get; init; } = ""; //GUID or UUID

    [Comment("ID of the referrer user who invited this user (referrer)")]
    public string? ReferrerId { get; set; } = "";

    public ICollection<ApiKey> ApiKeys { get; set; } = [];

    public static AppUser Create(AppUserCreateParams createParams)
    {
        if (string.IsNullOrWhiteSpace(createParams.Id))
            throw new ArgumentException("User ID cannot be empty", nameof(createParams.Id));

        if (string.IsNullOrWhiteSpace(createParams.Email))
            throw new ArgumentException("Email cannot be empty", nameof(createParams.Email));
        var user = new AppUser
        {
            Id = createParams.Id,
            Email = createParams.Email,
            UserName = createParams.Email, // Set UserName to Email by default
            PasswordHash = createParams.PasswordHash ,
            ReferralCode = GenerateReadableCode(6),
            ReferrerId = createParams.ReferrerId,
            ProviderName = createParams.ProviderName,
            EmailConfirmed = createParams.EmailConfirmed
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

