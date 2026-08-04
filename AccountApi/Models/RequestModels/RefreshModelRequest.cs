using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class RefreshModelRequest
{
    [Required(ErrorMessage = "RefreshToken is required")]
    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; set; }
}