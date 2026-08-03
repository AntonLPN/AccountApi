using Account.Application.Interfaces;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Infrastructure.Cryptography;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Services;
using Account.Infrastructure.Services.Email;
using Account.Infrastructure.Services.ExternalProviders;
using Ardalis.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Infrastructure.Extensions;

public static class DependencyInjectionInfrastructure
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICryptography, CryptographService>();
        services.AddScoped<IEmail, EmailService>();
        services.AddScoped<IProviderValidator, ProviderValidator>();
        services.AddScoped<IDataCache, RedisDataCache>();
        services.AddScoped<IMfaManager, MfaService>();
        services.AddScoped<IPreAuthTokenService, PreAuthTokenService>();
        //External Providers
        services.AddScoped<IUserAccountService, KeycloakAccountService>();
        services.AddScoped<IPasswordService, KeycloakPasswordService>();
        services.AddScoped<IGoogleAuthService, GoogleService>();
        //Repository
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWorkAdapter>();
        //MassTransit
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}