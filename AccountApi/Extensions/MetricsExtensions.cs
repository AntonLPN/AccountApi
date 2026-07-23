using OpenTelemetry.Metrics;

namespace AccountApi.Extensions;

public static class MetricsExtensions
{
    public static IServiceCollection AddObservabilityMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}
