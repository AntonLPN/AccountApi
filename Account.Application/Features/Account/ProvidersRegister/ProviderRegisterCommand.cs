using Account.Domain.Enums;
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace Account.Application.Features.Account.ProvidersRegister;

public record ProviderRegisterCommand(string GoogleToken, string ReferrerCode, AuthProviders Provider)
    : ICommand<Result<ProviderRegisterResult>>;