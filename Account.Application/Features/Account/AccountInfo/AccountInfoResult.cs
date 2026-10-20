using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.AccountInfo;

public class AccountInfoResult
{
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("surname")] public string Surname { get; set; } = "";

    [JsonPropertyName("emailConfirmed")] public bool EmailConfirmed { get; set; }

    [JsonPropertyName("referralCode")] public string? ReferralCode { get; set; } = "";

    [JsonPropertyName("isTwoFactorEnabled")]
    public bool IsTwoFactorEnabled { get; set; }

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastLoginAt")] public DateTime? LastLoginAt { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "Role";
    [JsonPropertyName("metadata")] public string Metadata { get; set; } = "";
}