using System.Text.Json.Serialization;
using Account.Domain.Models;

namespace Account.Application.Features.Account.Models;

public class BaseAuthResponse
{
    [JsonPropertyName("apiKey")] public string? ApiKey { get; init; } 
    public TokenResponse? Token { get; init; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}