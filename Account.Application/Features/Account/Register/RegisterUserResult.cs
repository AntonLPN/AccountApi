using System.Text.Json.Serialization;
using Account.Application.Features.Account.Models;

namespace Account.Application.Features.Account.Register;

public class RegisterUserResult
{
    [JsonPropertyName("testTest")] public string Test { get; set; }
}