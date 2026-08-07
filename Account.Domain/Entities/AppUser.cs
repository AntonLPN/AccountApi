using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Account.Domain.Events;
using Account.Domain.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace Account.Domain.Entities;

public class AppUser : AggregateRoot
{
    [Key] public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string EncryptedTwoFactorSecret { get; set; }
    public string? PasswordHash { get; set; } = "";

    public string? ProviderName { get; set; } = "my-corporate-ad"; //Google, Aple, etc.
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
            PasswordHash = createParams.PasswordHash,
            ReferralCode = GenerateReadableCode(),
            ReferrerId = createParams.ReferrerId,
            ProviderName = createParams.ProviderName,
            EmailConfirmed = createParams.EmailConfirmed,
            EncryptedTwoFactorSecret = Convert.ToBase64String(KeyGeneration.GenerateRandomKey(20))
        };
        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email));
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

    public void UpdateLastLogoutAt() => LastLogoutAt = DateTime.UtcNow;
    public void UpdateLastLoginAt() => LastLoginAt = DateTime.UtcNow;

    public void ChangePassword(string newHashPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(newHashPassword);
        PasswordHash = newHashPassword; // In a real application, you would hash the password before storing it
        AddDomainEvent(new PasswordChangedDomainEvent(Id));
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        AddDomainEvent(new EmailConfirmedDomainEvent(Id));
    }

    public void InitiateTwoFactorAuthentication(string otpCode)
    {
        AddDomainEvent(new TwoFactorInitiatedDomainEvent
        {
            UserId = Id,
            Email = Email,
            OtpCode = otpCode,
            CorrelationId = Guid.NewGuid(),
            ExpirationTime = DateTime.UtcNow.AddMinutes(5)
        });
    }
    public void RecordLogin(string? ipAddress, string? userAgent)
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInDomainEvent(Id, Email, ipAddress, userAgent));
    }
    
    public void EnableTwoFactorAuthentication()
    {
        IsTwoFactorEnabled = true;
        //TODO add domain event
    }
    public void DisableTwoFactorAuthentication()
    {
        IsTwoFactorEnabled = false;
        //TODO add domain event
    }
    
}