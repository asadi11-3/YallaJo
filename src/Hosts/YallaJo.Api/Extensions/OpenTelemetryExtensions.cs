using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace YallaJo.Api.Extensions;

/// <summary>
/// Configures OpenTelemetry tracing and metrics for the YallaJo API.
/// Instruments ASP.NET Core, HttpClient, and EF Core automatically.
/// </summary>
public static class OpenTelemetryExtensions
{
    private const string ServiceName = "YallaJo.Api";

    public static WebApplicationBuilder AddYallaJoOpenTelemetry(this WebApplicationBuilder builder)
    {
        var otelSection = builder.Configuration.GetSection("OpenTelemetry");
        var otlpEndpoint = otelSection["OtlpEndpoint"];

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: ServiceName,
                    serviceVersion: typeof(OpenTelemetryExtensions).Assembly
                        .GetName().Version?.ToString() ?? "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        // Filter out health check and swagger noise
                        opts.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("MediatR")    // Custom ActivitySource for MediatR pipeline
                    .AddSource("YallaJo.Outbox") // Outbox dispatch spans
                    .AddSource("YallaJo.Booking"); // Booking background service ticks

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }

                if (builder.Environment.IsDevelopment())
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("YallaJo.Outbox")    // Outbox metrics
                    .AddMeter("YallaJo.Booking");  // Booking background-service counters

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }

                if (builder.Environment.IsDevelopment())
                {
                    metrics.AddConsoleExporter();
                }
            });

        return builder;
    }
}
