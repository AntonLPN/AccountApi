using Account.Domain.Entities;

namespace Account.Domain.DTOs;

public sealed record AppUserCreateParams(
    AppUser.LanguageType Language,
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
    string? ProviderName = "my-corporate-ad");
