using Account.Application.Features.Account.Register;
using Account.Domain.Entities;
using Account.Infrastructure.Extensions;
using Account.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AccountApi.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddMySqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetSection("DbConfig:ConnectionString").Value;
        ArgumentException.ThrowIfNullOrEmpty(connectionString,
            "Database connection string configuration is missing or empty.");
        // var version = configuration.GetSection("DbConfig:VersionMySql").Value;
        // ArgumentException.ThrowIfNullOrEmpty(version, "Database version configuration is missing or empty.");
        var serverVersion = ServerVersion.AutoDetect(connectionString); // in this case catch exception when use docker
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, mySqlOptions => { mySqlOptions.CommandTimeout(30); })
        );

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                var keycloakSettings = configuration.GetSection("Authentication:Schemes:Bearer");
                options.Authority = keycloakSettings["Authority"];
                options.Audience = keycloakSettings["ValidAudience"];
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = keycloakSettings["ValidAudience"],
                    ValidateIssuer = true,
                    ValidIssuer = keycloakSettings["Authority"]
                };
            });
        
        return services;
    }

    public static IServiceCollection AddLifeTimeServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
            typeof(RegisterCommand).Assembly
        ));
        services.AddInfrastructureServices();
        return services;
    }
}