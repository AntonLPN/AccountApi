using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public sealed class RegisterModelRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("surname")] public string? Surname { get; set; }

    [JsonPropertyName("phoneNumber")]
    [Phone]
    [Required(ErrorMessage = "Phone number is required")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("referralCode")] public string? ReferralCode { get; set; }
}