using Account.Domain.Interfaces;
using Account.Domain.Models;
using Account.Infrastructure.Configuration;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Account.Infrastructure.Services.ExternalProviders;

public class GoogleService(IOptions<GoogleOptions> googleOptions, ILogger<GoogleService> logger) : IGoogleAuthService
{
    public async Task<GooglePayload> ValidateTokenAsync(string googleToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(googleToken);
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [googleOptions.Value.ClientId]
        };
        try
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
                googleToken,
                settings);

            return new GooglePayload()
            {
                Email = payload.Email,
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Google token validation failed");
            throw;
        }
    }
}