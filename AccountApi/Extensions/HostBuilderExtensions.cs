using Serilog;
using Serilog.Events;

namespace AccountApi.Extensions;

public static class HostBuilderExtensions
{
    public static void AddSerilogLogging(this IHostBuilder builder)
    {
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        builder.UseSerilog((context, loggerConfig) =>
        {
            var env = context.HostingEnvironment;

            loggerConfig
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", env.EnvironmentName)
                .Enrich.WithProperty("Application", "PRO_API")
                .WriteTo.Console(
                    outputTemplate: 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/pro-api-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}");

            loggerConfig.WriteTo.File(
                path: "logs/errors/pro-api-errors-.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);

            if (env.IsDevelopment())
            {
                loggerConfig.MinimumLevel.Debug();
            }
            else
            {
                loggerConfig.MinimumLevel.Information();
                loggerConfig.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                loggerConfig.MinimumLevel.Override("System", LogEventLevel.Warning);
            }
        });
    }
    
}