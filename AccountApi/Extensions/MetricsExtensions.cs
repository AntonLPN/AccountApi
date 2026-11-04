using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AccountApi.Extensions;

public static class MetricsExtensions
{
    public static IServiceCollection AddObservabilityMetrics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("AccountApi"))
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
                    // Если API запускается в Docker, задайте OpenTelemetry__Otlp__Endpoint=http://tempo:4317
                    options.Endpoint = new Uri(
                        configuration["OpenTelemetry:Otlp:Endpoint"] ?? "http://localhost:4317");
                    options.Protocol = OtlpExportProtocol.Grpc;
                }));

        return services;
    }
}
