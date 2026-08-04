using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class ConfirmEmailRequest
{
    [Required (ErrorMessage = "Code is required")]
    [JsonPropertyName("confirmationCode")]
    public required string ConfirmationCode { get; set; }
}