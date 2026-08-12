using Account.Application.Interfaces;
using Account.Domain.Enums;
using Account.Domain.Interfaces;

namespace Account.Infrastructure.Services;

public class ProviderValidator(IGoogleAuthService googleAuthService) : IProviderValidator
{
    public async Task<string?> ValidateProviderTokenAndGetEmailAsync(AuthProvider provider, string token)
    {
        switch (provider)
        {
            case AuthProvider.Google:
                var googlePayload = await googleAuthService.ValidateTokenAsync(token);
                return googlePayload.Email;
            case AuthProvider.Apple:
                //TODO waiting for apple implementation
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }
}