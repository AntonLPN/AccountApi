using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.AccountInfo;

public class AccountInfoResult
{
    [Required] [JsonPropertyName("email")] public string Email { get; set; } = "";

    [Required]
    [JsonPropertyName("emailConfirmed")]
    public bool EmailConfirmed { get; set; }

    [JsonPropertyName("referralCode")] public string? ReferralCode { get; set; } = "";

    [Required]
    [JsonPropertyName("isTwoFactorEnabled")]
    public bool IsTwoFactorEnabled { get; set; }

    [Required]
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastLoginAt")] public DateTime? LastLoginAt { get; set; }
}