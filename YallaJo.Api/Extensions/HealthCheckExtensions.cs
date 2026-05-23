using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using YallaJo.Api.HealthChecks;

namespace YallaJo.Api.Extensions;

/// <summary>
/// Configures health checks: liveness (app is running) and readiness
/// (app + dependencies are available). SQL Server connection is verified.
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddYallaJoHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddHealthChecks()
            .AddCheck("sqlserver", () =>
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1;";
                    command.ExecuteScalar();
                    return HealthCheckResult.Healthy("SQL Server is reachable.");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("SQL Server is unreachable.", ex);
                }
            }, tags: ["ready", "db"])
            .AddCheck("azure-translator", () =>
            {
                var endpoint = configuration["AzureTranslator:Endpoint"];
                return string.IsNullOrEmpty(endpoint)
                    ? HealthCheckResult.Degraded("AzureTranslator:Endpoint not configured")
                    : HealthCheckResult.Healthy($"Configured: {endpoint}");
            }, tags: ["ready", "external"])
            .AddCheck<OutboxDeadLetterHealthCheck>(
                "outbox-dead-letters",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "outbox", "ops"])
            .AddCheck<BookingBgServicesHealthCheck>(
                "booking-bg",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "booking", "bg"]);

        return services;
    }

    public static WebApplication MapYallaJoHealthChecks(this WebApplication app)
    {
        // Liveness: is the process alive? (K8s liveness probe)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteMinimalResponse,
        }).AllowAnonymous();

        // Readiness: are dependencies available? (K8s readiness probe)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteDetailedResponse,
        }).AllowAnonymous();

        // Legacy /health (backwards compatibility)
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedResponse,
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteMinimalResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
        });
    }

    private static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags,
            }),
        });
    }
}
