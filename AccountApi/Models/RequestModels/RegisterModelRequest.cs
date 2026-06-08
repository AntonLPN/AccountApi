using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public sealed class RegisterModelRequest
{
    [EmailAddress]
    [Required(ErrorMessage = "Email is required")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one number, and one special character.")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("referrerId")]
    [Required(ErrorMessage = "ReferrerId is required")]
    public string ReferrerId { get; set; } = "";
}