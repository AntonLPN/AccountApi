using System.Text.Json.Serialization;
using Account.Application.Features.Account.Models;

namespace Account.Application.Features.Account.Register;

public class RegisterUserResult
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}