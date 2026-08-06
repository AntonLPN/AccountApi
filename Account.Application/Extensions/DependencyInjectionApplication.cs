using Account.Application.Features.Account.Register;
using Account.Application.Interfaces;
using Account.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Application.Extensions;

public static class DependencyInjectionApplication
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILoginStrategy, MfaLoginStrategy>();
        services.AddScoped<ILoginStrategy, StandardLoginStrategy>();
        services.AddScoped<IUserRegistrationCoordinator, UserRegistrationCoordinator>();
    }
}