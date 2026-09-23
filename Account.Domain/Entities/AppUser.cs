using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Account.Domain.Events;
using Account.Domain.Models;
using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace Account.Domain.Entities;

public class AppUser : AggregateRoot
{
    [Key] public Guid Id { get; init; }
    public string? UserName { get; init; }
    public string Email { get; init; } = "";
    public bool EmailConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? EncryptedTwoFactorSecret { get; init; }
    public string? PasswordHash { get; set; } = "";

    public string? ProviderName { get; set; } = "my-corporate-ad"; //Google, Aple, etc.
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastLogoutAt { get; set; }

    [Comment(
        "Unique code that the user can use to invite others. Automatically generated when the user is created.")]
    public string ReferralCode { get; init; } = ""; //GUID or UUID

    [Comment("ID of the referrer user who invited this user (referrer)")]
    public Guid? ReferrerId { get; set; }

    public bool IsDeleted { get; set; }
    // Navigation properties
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<LoginAudit> LoginAudits { get; set; } = [];
    public ICollection<LogoutAudit> LogoutAudits { get; set; } = [];
    public ICollection<OtpSessions> OtpSessions { get; set; } = [];

    public static AppUser Create(AppUserCreateParams createParams)
    {
        Guard.Against.Null(createParams);
        Guard.Against.NullOrWhiteSpace(createParams.Id, nameof(createParams.Id));

        if (!Guid.TryParse(createParams.Id, out var userId))
            throw new ArgumentException("Id must be valid GUID", nameof(createParams.Id));

        Guard.Against.Default(userId, nameof(createParams.Id));
        var email = Guard.Against.NullOrWhiteSpace(createParams.Email, nameof(createParams.Email));

        var user = new AppUser
        {
            Id = Guard.Against.Default(Guid.Parse(createParams.Id), nameof(createParams.Id)),
            Email = email,
            UserName = email,
            PasswordHash = createParams.PasswordHash,
            ReferralCode = GenerateReadableCode(),
            ReferrerId = createParams.ReferrerId,
            ProviderName = createParams.ProviderName,
            EmailConfirmed = createParams.EmailConfirmed,
            EncryptedTwoFactorSecret = Convert.ToBase64String(KeyGeneration.GenerateRandomKey(20)),
            CreatedAt = DateTime.UtcNow
        };
        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email, createParams.IpAddress,
            createParams.UserAgent));
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


    public void ChangePassword(string newHashPassword)
    {
        Guard.Against.NullOrWhiteSpace(newHashPassword);
        PasswordHash = newHashPassword;
        AddDomainEvent(new PasswordChangedDomainEvent(Id));
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        AddDomainEvent(new EmailConfirmedDomainEvent(Id));
    }

    public void InitiateTwoFactorAuthentication(string otpCode)
    {
        EnsureActiveAccount();
        AddDomainEvent(new TwoFactorInitiatedDomainEvent
        {
            CorrelationId = Guid.NewGuid(),
            UserId = Id,
            Email = Email,
            OtpCode = Guard.Against.NullOrWhiteSpace(otpCode),
            ExpirationTime = DateTime.UtcNow.AddMinutes(5)
        });
    }

    public void RecordLogin(string? ipAddress, string? userAgent)
    {
        EnsureActiveAccount();
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedInDomainEvent(Id, Email, ipAddress, userAgent));
    }

    #region these methods can use user and administrator to change the status of the user

    public void Logout(string? ipAddress, string? userAgent)

    {
        LastLogoutAt = DateTime.UtcNow;
        AddDomainEvent(new UserLoggedOutDomainEvent(Id, Email, ipAddress, userAgent));
    }

    public void SetTwoFactor(bool isEnable)
    {
        IsTwoFactorEnabled = isEnable;
    }

    #endregion


    private void EnsureActiveAccount()
    {
        if (IsBlocked)
            throw new InvalidOperationException("User is blocked");
        if (IsDeleted)
            throw new InvalidOperationException("User is deleted");
    }
}