using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

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
                .AddPrometheusExporter())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation() 
                .AddSource("MassTransit") 
                .AddOtlpExporter(options =>
                {
                    //TODO add grafana tempo(container) for tracing and metrics
                    //options.Endpoint = new Uri(configuration["OpenTelemetry:Otlp:Endpoint"] ?? "http://localhost:4317");
                   // options.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
