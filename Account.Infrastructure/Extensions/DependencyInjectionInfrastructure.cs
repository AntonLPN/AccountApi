using Account.Application.Interfaces;
using Account.Domain.Interfaces;
using Account.Domain.Repositories;
using Account.Infrastructure.Cryptography;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Repositories;
using Account.Infrastructure.Services;
using Account.Infrastructure.Services.Email;
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
        //Repository
        services.AddScoped<IUnitOfWork, UnitOfWorkAdapter>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ILoginAuditRepository, LoginAuditRepository>();
        services.AddScoped<ILogoutAuditRepository, LogoutAuditRepository>();
        //MassTransit
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
    }
}