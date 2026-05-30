// Auth.Presentation/RateLimiting/RateLimitPolicies.cs
//
// NOTE ON LOCATION: The task spec requested YallaJo.Api/RateLimiting/RateLimitPolicies.cs,
// but that creates a circular project dependency:
//   YallaJo.Api → Auth.Presentation → (would need) → YallaJo.Api  ✗
//
// Auth.Presentation already has <FrameworkReference Include="Microsoft.AspNetCore.App" />
// which includes Microsoft.AspNetCore.RateLimiting, so the extension method lives here.
// YallaJo.Api already references Auth.Presentation, so Program.cs can call
// AddYallaJoRateLimiting() via the existing 'using Auth.Presentation;' import.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Auth.Presentation;

public static class RateLimitPolicies
{
    public const string LoginPolicy    = "login-rate-limit";
    public const string OtpPolicy      = "otp-rate-limit";
    public const string RefreshPolicy  = "refresh-rate-limit";
    public const string RegisterPolicy = "register-rate-limit";

    /// <summary>
    /// Registers per-endpoint rate limiting policies for all anonymous authentication
    /// endpoints. Call this before AddAuthentication/AddAuthorization in Program.cs.
    /// UseRateLimiter() must be inserted into the pipeline before UseAuthentication().
    /// </summary>
    public static IServiceCollection AddYallaJoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global 429 rejection — RFC 7807-compatible JSON body
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"error": "Too many requests. Please try again later."}""", ct);
            };

            // ── Login: 10 req / 1 min per IP — fixed window ───────────────────
            options.AddPolicy(LoginPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit            = 10,
                        Window                 = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder   = QueueProcessingOrder.OldestFirst,
                        QueueLimit             = 0
                    }));

            // ── OTP: 3 req / 1 min per email (or IP fallback) — sliding window ─
            // EnableBuffering() is called so the endpoint can still read the body.
            options.AddPolicy(OtpPolicy, httpContext =>
            {
                var key = ExtractEmailOrIp(httpContext);
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: key,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit          = 3,
                        Window               = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow    = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    });
            });

            // ── Refresh: 20 req / 1 min per IP — fixed window ─────────────────
            options.AddPolicy(RefreshPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = 20,
                        Window               = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    }));

            // ── Register: 5 req / 1 min per IP — fixed window ────────────────
            options.AddPolicy(RegisterPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit          = 5,
                        Window               = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit           = 0
                    }));
        });

        return services;
    }

    /// <summary>
    /// Buffers the request body and extracts the "email" JSON field for per-account
    /// OTP throttling. Resets the body stream so the endpoint can read it normally.
    /// Falls back to the client IP when the email cannot be determined.
    /// </summary>
    private static string ExtractEmailOrIp(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            // EnableBuffering replaces the raw Kestrel stream with a seekable
            // FileBufferingReadStream / MemoryStream — safe to read and rewind.
            httpContext.Request.EnableBuffering();
            httpContext.Request.Body.Position = 0;

            using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);

            // ReadToEndAsync on an already-buffered MemoryStream completes
            // synchronously, so .GetAwaiter().GetResult() will not deadlock.
            var body = reader.ReadToEndAsync().GetAwaiter().GetResult();

            // Rewind so the endpoint model-binder can read the body again.
            httpContext.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
                return ip;

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("email", out var emailProp))
            {
                var email = emailProp.GetString()?.Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(email))
                    return email;
            }
        }
        catch
        {
            // JSON parse failure, empty body, stream error → fall through to IP
        }

        return ip;
    }
}
