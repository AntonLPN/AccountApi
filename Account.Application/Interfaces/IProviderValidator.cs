using Account.Domain.Enums;

namespace Account.Application.Interfaces;

public interface IProviderValidator
{
    Task<string?> ValidateProviderTokenAndGetEmailAsync(AuthProviders provider, string token);
}