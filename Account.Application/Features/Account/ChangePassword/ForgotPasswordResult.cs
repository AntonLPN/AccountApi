using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.ChangePassword;

public class ForgotPasswordResult
{
    [JsonPropertyName("accessToken")] public required string AccessToken { get; init; }
    [JsonPropertyName("pendingToken")] public required string PendingToken { get; init; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}