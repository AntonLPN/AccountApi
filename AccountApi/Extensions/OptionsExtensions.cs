using Account.Infrastructure.Configuration;

namespace AccountApi.Extensions;

public static class OptionsExtensions
{
    public static WebApplicationBuilder AddOptions(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<KeycloakAdminOptions>().BindConfiguration("KeycloakAdminClient")
            .ValidateDataAnnotations().ValidateOnStart();
        builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
        builder.Services.Configure<CryptoOptions>(builder.Configuration.GetSection("Crypto"));
        builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection("ApiKey"));
        builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
        builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));
        builder.Services.Configure<AppUrlOptions>(builder.Configuration.GetSection("AppUrl"));

        return builder;
    }
}