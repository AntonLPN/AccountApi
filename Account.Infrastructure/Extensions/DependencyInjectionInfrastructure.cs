using Account.Application.Interfaces;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Infrastructure.Cryptography;
using Account.Infrastructure.MassTransit;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Services;
using Account.Infrastructure.Services.Email;
using Account.Infrastructure.Services.ExternalProviders;
using Ardalis.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using IDomainEventDispatcher = Account.Domain.Interfaces.IDomainEventDispatcher;

namespace Account.Infrastructure.Extensions;

public static class DependencyInjectionInfrastructure
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, KeycloakAuthService>();
        services.AddScoped<ICryptography, CryptographService>();
        services.AddScoped<IEmail, EmailService>();
        services.AddScoped<IProviderValidator, ProviderValidator>();
        services.AddScoped<IDataCache, RedisDataCache>();
        services.AddScoped<IMfaManager, MfaService>();
        services.AddScoped<IPreAuthTokenService, PreAuthTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        //External Providers
        services.AddScoped<IUserAccountService, KeycloakAccountService>();
        services.AddScoped<IProviderPasswordService, KeycloakProviderPasswordService>();
        services.AddScoped<IGoogleAuthService, GoogleService>();
        //Repository
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWorkAdapter>();
        //MassTransit
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        services.AddScoped<IOutboxEventPublisher, MassTransitOutboxEventPublisher>();
        //Domain
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }
}