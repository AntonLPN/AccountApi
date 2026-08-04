using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class GoogleLoginModelRequest
{
    [Required(ErrorMessage = "Token is required")]
    [JsonPropertyName("token")]
    public required string Token { get; set; } 

}