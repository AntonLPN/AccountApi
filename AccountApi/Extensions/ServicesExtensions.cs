using Account.Application.Common.Behaviors;
using Account.Application.Extensions;
using Account.Application.Features.Account.Register;
using Account.Domain.Extensions;
using Account.Infrastructure.Extensions;
using Account.Infrastructure.MassTransit;
using MediatR;


namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(RegisterCommand).Assembly,
                typeof(MassTransitIntegrationEventPublisher).Assembly // for integration events triggers

            );
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });
           
        
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddDomainServices();

        return services;
    }
}