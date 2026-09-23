using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class ChangePasswordRequestModel
{
    [JsonPropertyName("newPassword")]
    [Required(ErrorMessage = "NewPassword is required")]
    public required string NewPassword { get; set; }

    [JsonPropertyName("pendingToken")]
    [Required(ErrorMessage = "PendingToken is required")]
    public required string PendingToken { get; set; }

}