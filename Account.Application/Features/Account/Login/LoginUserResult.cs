using System.Text.Json.Serialization;
using Account.Application.Features.Account.Models;

namespace Account.Application.Features.Account.Login;

public class LoginUserResult : BaseAuthResponse
{
    [JsonPropertyName("mfaStateToken")] public string? MfaStateToken { get; set; }
    [JsonPropertyName("isMfaRequired")] public bool IsMfaRequired { get; set; }
}