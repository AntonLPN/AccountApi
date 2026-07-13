using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class ForgotPasswordRequestModel
{
    [JsonPropertyName("email")]
    [EmailAddress]
    [Required(ErrorMessage = "Email is required")]
    public required string Email { get; set; }
}