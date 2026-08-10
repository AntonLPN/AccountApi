using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountApi.Extensions;

public static class DataBaseExtensions
{
    public static IServiceCollection AddMySqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
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
}