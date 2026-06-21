using Account.Domain.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace Account.Domain.Extensions;

public static class DependencyInjectionDomain
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(LoginAuditMapper).Assembly));
    }
}