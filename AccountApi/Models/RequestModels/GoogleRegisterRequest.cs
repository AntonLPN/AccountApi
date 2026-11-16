using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Account.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountApi.Models.RequestModels;

public class GoogleRegisterRequest
{
    [Required(ErrorMessage = "Token is required")]
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    [Required(ErrorMessage = "ReferralCode is required")]
    [SwaggerSchema(
        "Can be empty. If the user was referred by someone, then this field should contain the referral code of that person."
    )]
    [JsonPropertyName("referralCode")]
    public string ReferralCode { get; set; } = "";

    [JsonPropertyName("language")] public AppUser.LanguageType Language { get; set; }
}