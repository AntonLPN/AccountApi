using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class OtpCodeVerificationRequestModel
{
    [Required(ErrorMessage = "OtpCode is required.")]
    [JsonPropertyName("otpCode")]
    public required string OtpCode { get; set; }
    
    [Required(ErrorMessage = "PendingToken is required.")]
    [JsonPropertyName("pendingToken")]
    public required string PendingToken { get; set; }
}