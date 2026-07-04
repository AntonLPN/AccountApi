using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccountApi.Models.RequestModels;

public class OtpCodeVerificationRequestModel
{
    [JsonPropertyName("otpCode")]
    [Required(ErrorMessage = "OtpCode is required.")]
    public required string OtpCode { get; set; }
}