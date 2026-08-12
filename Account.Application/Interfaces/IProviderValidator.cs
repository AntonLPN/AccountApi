using Account.Domain.Enums;

namespace Account.Application.Interfaces;

public interface IProviderValidator
{
    Task<string?> ValidateProviderTokenAndGetEmailAsync(AuthProvider provider, string token);
}