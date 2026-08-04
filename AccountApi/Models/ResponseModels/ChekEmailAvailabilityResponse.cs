using System.Text.Json.Serialization;

namespace AccountApi.Models.ResponseModels;

public class ChekEmailAvailabilityResponse
{
    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }
}