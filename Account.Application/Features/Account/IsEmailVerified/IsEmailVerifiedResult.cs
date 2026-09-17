using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.IsEmailVerified;

public class IsEmailVerifiedResult
{
    [Required] [JsonPropertyName("email")] public string Email { get; set; } = "";

    [Required]
    [JsonPropertyName("isEmailVerified")]
    public bool IsEmailVerified { get; set; }
}
