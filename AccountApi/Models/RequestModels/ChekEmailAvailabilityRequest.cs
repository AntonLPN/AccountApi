using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class ChekEmailAvailabilityRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}