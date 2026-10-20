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
    string Name = "",
    string Surname = "",
    string? ProviderName = "my-corporate-ad",
    JsonObject? Metadata = null);
