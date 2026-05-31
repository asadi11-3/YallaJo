using System.Diagnostics;

namespace YallaJo.Api.Middleware;

/// <summary>
/// Adds baseline security and observability response headers to every response.
/// F114 fix: prior to this, responses carried none of the standard hardening
/// headers (CSP, X-Frame-Options, HSTS, X-Content-Type-Options, X-XSS-Protection)
/// nor a correlation id, leaving the API exposed to clickjacking / MIME-sniffing
/// and making cross-system tracing harder.
///
/// Headers are written via <see cref="HttpResponse.OnStarting"/> so they are
/// applied just before the response is flushed, regardless of what downstream
/// middleware/endpoints do, and never throw "headers already sent".
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            // Prevent MIME-type sniffing.
            headers["X-Content-Type-Options"] = "nosniff";

            // Disallow framing (clickjacking protection). frame-ancestors in CSP
            // is the modern equivalent but X-Frame-Options is still widely checked.
            headers["X-Frame-Options"] = "DENY";

            // Legacy header; modern guidance (OWASP) is to disable the buggy
            // legacy auditor with "0" and rely on CSP instead.
            headers["X-XSS-Protection"] = "0";

            // Limit referrer leakage.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict powerful browser features by default.
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // HSTS only makes sense over HTTPS.
            if (ctx.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Content-Security-Policy:
            //   • Swagger UI (Development only) needs inline scripts/styles to render,
            //     so relax CSP for the /swagger branch.
            //   • Everything else (the JSON API surface) gets a locked-down policy.
            if (ctx.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] =
                    "default-src 'self'; img-src 'self' data: https:; " +
                    "script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
                    "connect-src 'self'";
            }
            else
            {
                headers["Content-Security-Policy"] =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            }

            // Observability: surface the trace id so clients can correlate a
            // response with server-side logs/traces.
            var traceId = Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
            headers["X-Correlation-ID"] = traceId;

            var traceParent = Activity.Current?.Id;
            if (!string.IsNullOrEmpty(traceParent))
            {
                headers["traceparent"] = traceParent;
            }

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
