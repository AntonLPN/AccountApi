using System.Text;
using AccountApi.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
// ReSharper disable InconsistentNaming

namespace AccountApi.Extensions;

public static class AuthenticationExtensions
{
    private const string PRE_AUTH_SHEME = "PreAuth";
    private const string ISUER_NAME = "account-api-preauth";
    private const string AUDIENCE_NAME = "account-api-preauth";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSettings = configuration.GetSection("Authentication:Schemes:Bearer");
        var preAuthKey = configuration["Authentication:PreAuth:SigningKey"]
                         ?? throw new InvalidOperationException(
                             "Authentication:PreAuth:SigningKey configuration is missing.");
        bool allowInsecureHttp = configuration.GetValue<bool>("Authentication:AllowInsecureHttp");

        services.AddAuthentication("Bearer")
            //Keycloak - main authentication scheme, for all endpoints except /verify-otp
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = keycloakSettings["Authority"] ??
                                    throw new InvalidOperationException("Authority for keycloak settings is missing.");
                options.Audience = keycloakSettings["ValidAudience"] ??
                                   throw new InvalidOperationException($"ValidAudience for keycloak is missing.");
                options.RequireHttpsMetadata = !allowInsecureHttp;
                options.MapInboundClaims = false;
#if DEBUG
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"Bearer auth FAILED: {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
#endif
            })
            //PreAuth - second authentication scheme, for /verify-otp
            .AddJwtBearer(PRE_AUTH_SHEME, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = ISUER_NAME,
                    ValidateAudience = true,
                    ValidAudience = AUDIENCE_NAME,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(preAuthKey)),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
#if DEBUG
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine($"PreAuth FAILED: {ctx.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };
#endif
            })
            .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(AuthPolicies.ApiKeyOnly, _ => { });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.ApiKeyOnly, policy => policy
                .AddAuthenticationSchemes(AuthPolicies.ApiKeyOnly)
                .RequireAuthenticatedUser())

            //for /verify-otp
            .AddPolicy(AuthPolicies.PreAuthOnly, policy => policy
                .AddAuthenticationSchemes(PRE_AUTH_SHEME)
                .RequireAuthenticatedUser()
                .RequireClaim("purpose",
                    "otp_pending"))

            //for all other endpoints, require full authentication through Keycloak
            .AddPolicy(AuthPolicies.MfaRequired, policy => policy
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser())
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes("Bearer")
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }
}