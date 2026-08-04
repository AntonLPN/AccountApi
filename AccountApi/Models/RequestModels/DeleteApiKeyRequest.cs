using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class DeleteApiKeyRequest
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;
}