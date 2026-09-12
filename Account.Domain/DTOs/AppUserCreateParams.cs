namespace Account.Domain.Models;

public sealed record AppUserCreateParams(
    string Id,
    string Email,
    string? PasswordHash,
    Guid? ReferrerId,
    string? IpAddress,
    string? UserAgent,
    bool EmailConfirmed = false,
    string? ProviderName = "my-corporate-ad");
