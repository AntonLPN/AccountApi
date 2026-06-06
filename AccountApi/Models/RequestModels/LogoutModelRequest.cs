using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public sealed class LogoutModelRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = "";
}