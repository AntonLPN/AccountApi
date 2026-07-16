using Account.Application.Interfaces;
using Account.Domain.Enums;
using Account.Domain.Interfaces;

namespace Account.Infrastructure.Services;

public class ProviderValidator(IGoogleAuthService googleAuthService) : IProviderValidator
{
    public async Task<string?> ValidateProviderTokenAndGetEmailAsync(AuthProviders provider, string token)
    {
        switch (provider)
        {
            case AuthProviders.Google:
                var googlePayload = await googleAuthService.ValidateTokenAsync(token);
                return googlePayload.Email;
            case AuthProviders.Apple:
                //TODO waiting for apple implementation
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }
}