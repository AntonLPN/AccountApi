using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ProvidersRegister;

public record ProviderRegisterCommand(
    string ProviderToken,
    string ReferrerCode,
    AuthProviders Provider,
    string? IpAddress,
    string? UserAgent)
    : ICommand<Result<ProviderRegisterResult>>;