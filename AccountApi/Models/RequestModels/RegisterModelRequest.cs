using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountApi.Models.RequestModels;

public sealed class RegisterModelRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "ReferralCode is required")]
    [SwaggerSchema(
        "Can be empty. If the user was referred by someone, then this field should contain the referral code of that person.")]
    [JsonPropertyName("referralCode")]
    public required string ReferralCode { get; set; }
}