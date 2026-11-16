using Account.Domain.Entities;
using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ProvidersRegister;

public record ProviderRegisterCommand(
    string ProviderToken,
    string? ReferrerCode,
    AuthProvider Provider,
    string? IpAddress,
    string? UserAgent,
    AppUser.LanguageType Language)
    : ICommand<Result<ProviderRegisterResult>>;