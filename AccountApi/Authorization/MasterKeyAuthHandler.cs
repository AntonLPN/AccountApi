using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Account.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AccountApi.Authorization;

public class MasterKeyAuthHandler(
    IOptionsMonitor<MasterKeyAuthSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyOptions> apiKeyOptions)
    : AuthenticationHandler<MasterKeyAuthSchemeOptions>(options, logger, encoder)
{
    private readonly string _masterApiKey = apiKeyOptions.Value.Key;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!IsMasterKey(keyHeader.ToString()))
            return Task.FromResult(AuthenticateResult.Fail("Invalid master key."));

        var claims = new[] { new Claim(ClaimTypes.Role, "System") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsMasterKey(string apiKey)
    {
        var apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
        var masterKeyBytes = Encoding.UTF8.GetBytes(_masterApiKey);

        if (apiKeyBytes.Length != masterKeyBytes.Length)
            return false;
        return apiKeyBytes.SequenceEqual(masterKeyBytes);
    }
}

public class MasterKeyAuthSchemeOptions : AuthenticationSchemeOptions
{
}