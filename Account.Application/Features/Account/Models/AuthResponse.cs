using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.Models;

public class AuthResponse
{
    [JsonPropertyName("api_key")] public string ApiKey { get; set; } = "";
    [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("tokenType")] public string TokenType { get; set; } = "Bearer";
}