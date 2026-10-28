using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class GenerateOtpCodeRequestModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    [MaxLength(255)]
    [MinLength(5)]
    [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Email is not valid")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}