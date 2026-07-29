namespace Account.Domain.Models;

public sealed record AppUserCreateParams(
    string Id,
    string Email,
    string? PasswordHash,
    string? ReferrerId,
    bool EmailConfirmed = false,
    string? ProviderName = "my-corporate-ad");