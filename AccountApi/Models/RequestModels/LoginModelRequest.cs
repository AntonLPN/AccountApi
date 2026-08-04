using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public sealed class LoginModelRequest
{
    [EmailAddress]
    [Required(ErrorMessage = "Email is required")]
    [JsonPropertyName("email")]
    public required string Email { get; set; } 

    [Required(ErrorMessage = "Password is required")]
    [JsonPropertyName("password")]
    public required string Password { get; set; } 
}