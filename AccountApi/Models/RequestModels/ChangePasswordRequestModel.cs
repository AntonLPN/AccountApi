using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class ChangePasswordRequestModel
{
    [JsonPropertyName("email")] public string Email { get; set; } = null!;
    [JsonPropertyName("newPassword")] public string NewPassword { get; set; } = null!;
    [JsonPropertyName("resetToken")] public string ResetToken { get; set; } = null!;
}