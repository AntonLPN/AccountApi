using Account.Domain.Interfaces;
using Account.Infrastructure.Cryptography;
using Account.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Infrastructure.Extensions;

public static class DependencyInjectionInfrastructure
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, KeycloakAuthService>();
        services.AddScoped<ICryptography, CryptographService>();
    }
}