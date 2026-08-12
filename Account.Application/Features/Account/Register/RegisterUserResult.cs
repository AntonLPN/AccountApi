using System.Text.Json.Serialization;
using Account.Application.Features.Account.Models;

namespace Account.Application.Features.Account.Register;

public class RegisterUserResult
{
    [JsonPropertyName("isSuccess")] public bool IsSuccess { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}