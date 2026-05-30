using Serilog;
using Serilog.Events;

namespace YallaJo.Api.Extensions;

/// <summary>
/// Configures Serilog as the logging provider with structured logging,
/// environment enrichment, and appropriate sinks per environment.
/// </summary>
public static class SerilogExtensions
{
    public static WebApplicationBuilder AddYallaJoSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            var env = context.HostingEnvironment;

            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "YallaJo.Api");

            // ── Console sink (always) ─────────────────────────────────────
            if (env.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                // Structured JSON for production log aggregation (Kibana, Seq, etc.)
                loggerConfiguration.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
            }

            // ── File sink (rolling, always) ───────────────────────────────
            loggerConfiguration.WriteTo.File(
                path: "logs/yallajo-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}");

            // ── Minimum levels ────────────────────────────────────────────
            loggerConfiguration
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);

            if (env.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
            }
            else
            {
                loggerConfiguration.MinimumLevel.Information();
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds Serilog request logging middleware with enriched context.
    /// Call after UseRouting, before UseEndpoints.
    /// </summary>
    public static WebApplication UseYallaJoSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value;
                    if (userId is not null)
                    {
                        diagnosticContext.Set("UserId", userId);
                    }
                }
            };
        });

        return app;
    }
}
