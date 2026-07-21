using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Account.Domain.Interfaces;
using Account.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Account.Infrastructure.Services;

public class PreAuthTokenService(IOptions<AuthenticationOptions> authenticationOptions, IDataCache dataCache)
    : IPreAuthTokenService
{
    private const string KeyNamePrefix = "pending_token_";
    public string GeneratePreAuthToken(string email)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(authenticationOptions.Value.PreAuth.SigningKey));
        int lifeTime = 5;
#if DEBUG
        lifeTime = 60;
#endif
        var claims = new[]
        {
            new Claim("email", email),
            new Claim("purpose", "otp_pending")
        };

        var token = new JwtSecurityToken(
            issuer: "account-api-preauth",
            audience: "account-api-preauth",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifeTime), //TTL OTP
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GeneratePendingTokenAsync(string email)
    {
        var pendingToken = Guid.NewGuid().ToString("N");
   
        await dataCache.SetStringAsync($"{KeyNamePrefix+pendingToken}", email, TimeSpan.FromMinutes(5));
        return pendingToken;
    }

    public async Task<bool> ValidatePendingTokenAsync(string pendingToken,string email)
    {
        var res = await dataCache.ConsumeAsync($"{KeyNamePrefix+pendingToken}");
        if (string.IsNullOrEmpty(res))
        {
            return false;
        }
        return res.Equals(email, StringComparison.OrdinalIgnoreCase);;
    }
}