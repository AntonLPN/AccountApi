using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class Enable2FaSetupRequest
{
    [Required]
    [JsonPropertyName("isEnableTwoFactor")]
    public bool IsEnableTwoFactor { get; set; }
}