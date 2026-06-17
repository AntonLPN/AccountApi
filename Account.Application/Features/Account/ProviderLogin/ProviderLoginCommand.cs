using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ProviderLogin;

public record ProviderLoginCommand(
    string ProviderToken,
    AuthProviders Provider,
    string? IpAddress,
    string? UserAgent)
    : ICommand<Result<ProviderLoginResult>>;