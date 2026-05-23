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
                // Обогащение логов metadata
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", env.EnvironmentName)
                .Enrich.WithProperty("Application", "PRO_API")
                
                // Форматирование логов
                .WriteTo.Console(
                    outputTemplate: 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                
                // File logging с rotation
                .WriteTo.File(
                    path: "logs/pro-api-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: 
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}");

            // Error logs в отдельный файл
            loggerConfig.WriteTo.File(
                path: "logs/errors/pro-api-errors-.txt",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);

            // Настройка уровней логирования
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