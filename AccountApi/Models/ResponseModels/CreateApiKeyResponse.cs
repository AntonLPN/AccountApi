using System.Text.Json.Serialization;

namespace AccountApi.Models.ResponseModels;

public class CreateApiKeyResponse
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;
}