using Account.Application.Common.Behaviors;
using Account.Application.Features.Account.Register;
using Account.Infrastructure.MassTransit;
using MediatR;

namespace AccountApi.Extensions;

public static class MediatorExtensions
{
    public static IServiceCollection AddMediatorServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(RegisterCommand).Assembly,
                typeof(MassTransitIntegrationEventPublisher).Assembly // for integration events triggers
            );
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }
}