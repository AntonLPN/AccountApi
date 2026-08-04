using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class DeleteApiKeyRequest
{
    [Required(ErrorMessage = "ApiKey is required")]
    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; set; }
}