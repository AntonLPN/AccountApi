using System.Text.Json.Serialization;

namespace Account.Application.Features.Account.ChangePassword;

public class ChangePasswordResult
{
    [JsonPropertyName("isPasswordChanged")]
    public bool IsPasswordChanged { get; set; }
}