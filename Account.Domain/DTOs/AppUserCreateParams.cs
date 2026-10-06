using System.Text.Json.Nodes;

namespace Account.Domain.Models;

public sealed record AppUserCreateParams(
    string Id,
    string Email,
    string PhoneNumber,
    string? PasswordHash,
    Guid? ReferrerId,
    string? IpAddress,
    string? UserAgent,
    bool EmailConfirmed = false,
    string? Name = null,
    string? Surname = null,
    string? ProviderName = "my-corporate-ad",
    JsonObject? Metadata = null);
