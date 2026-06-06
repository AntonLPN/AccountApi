using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public sealed class LogoutModelRequest
{
    [EmailAddress]
    [Required(ErrorMessage = "Email is required")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Refresh token is required")]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";
}