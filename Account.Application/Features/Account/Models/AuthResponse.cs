using System.Text.Json.Serialization;
using Account.Domain.Models;

namespace Account.Application.Features.Account.Models;

public class AuthResponse
{
    [JsonPropertyName("apiKey")] public string ApiKey { get; set; } = "";
    public TokenResponse Token { get; set; } = new();

}