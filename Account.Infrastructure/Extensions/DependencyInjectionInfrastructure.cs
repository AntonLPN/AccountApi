using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Infrastructure.Cryptography;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Repositories;
using Account.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Infrastructure.Extensions;

public static class DependencyInjectionInfrastructure
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, KeycloakAuthService>();
        services.AddScoped<ICryptography, CryptographService>();
        //Repository
        services.AddScoped<IUnitOfWork, UnitOfWorkAdapter>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        //MassTransit 
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}