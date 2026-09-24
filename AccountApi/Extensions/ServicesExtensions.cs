using Account.Application.Extensions;
using Account.Domain.Extensions;
using Account.Infrastructure.Extensions;


namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddDomainServices();

        return services;
    }
}