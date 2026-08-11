using System.Text;
using Account.Application.Extensions;
using Account.Application.Features.Account.Register;
using Account.Domain.Extensions;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Extensions;
using Account.Infrastructure.MassTransit;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.SagaModels;
using Account.Infrastructure.Saga.TwoFactor;
using Account.Infrastructure.Saga.UserRegister;
using Account.Infrastructure.Services;
using AccountApi.Authorization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(RegisterCommand).Assembly,
            typeof(MassTransitIntegrationEventPublisher).Assembly // for integration events triggers
        ));
        services.AddInfrastructureServices();
        services.AddApplicationServices();
        services.AddDomainServices();

        return services;
    }
}