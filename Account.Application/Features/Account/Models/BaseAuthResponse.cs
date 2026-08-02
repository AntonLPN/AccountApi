using System.Text.Json.Serialization;
using Account.Domain.Entities;
using Account.Domain.Models;

namespace Account.Application.Features.Account.Models;

public class BaseAuthResponse
{
    [JsonPropertyName("apiKeys")] public List<string> ApiKeys { get; init; } = [];
    public TokenResponse? Token { get; init; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}