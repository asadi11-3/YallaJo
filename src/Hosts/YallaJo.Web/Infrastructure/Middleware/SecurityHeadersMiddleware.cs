using System.Diagnostics;

namespace YallaJo.Web.Infrastructure.Middleware;

// (Below) the connect-src directive must allowlist the cross-origin API host so the
// browser can reach the API-hosted SignalR hub (/hubs/tour) used by the §2.5 public
// tour live-slots feature: the SignalR negotiate XHR + the WebSocket (wss://) are
// blocked by a 'self'-only connect-src. Origins are derived from configuration
// ("ApiBaseUrl") via System.Uri, never hardcoded.

/// <summary>
/// GAP-7 (RBAC fix-code audit, rule SEC2): adds the baseline hardening response
/// headers to every Web (BFF) response. Prior to this the Web host pipeline only
/// called <c>UseHsts()</c> (Program.cs), so the Razor surface — including the §9
/// RBAC console — shipped without X-Content-Type-Options, X-Frame-Options,
/// Referrer-Policy, Permissions-Policy or a Content-Security-Policy, leaving it
/// exposed to clickjacking / MIME-sniffing. This mirrors the API host's
/// <c>YallaJo.Api.Middleware.SecurityHeadersMiddleware</c>, but uses a CSP tuned
/// for a server-rendered MVC app (the API's <c>default-src 'none'</c> policy would
/// blank the entire UI).
///
/// Headers are written via <see cref="HttpResponse.OnStarting"/> so they are
/// applied just before the response is flushed, regardless of what downstream
/// middleware/endpoints do, and never throw "headers already sent".
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // connect-src allowlist tokens for the cross-origin API host (SignalR /hubs/tour).
    // Computed once from configuration["ApiBaseUrl"]; e.g. "https://localhost:57065 wss://localhost:57065".
    private readonly string _apiConnectSources;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var apiBaseUrl = configuration["ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl)
            || !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri)
            || (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Configuration value 'ApiBaseUrl' is missing or is not an absolute http/https URL; " +
                "it is required to build the Content-Security-Policy connect-src for the SignalR hub.");
        }

        // scheme://host[:port] — Uri normalizes default ports and drops any path/trailing slash.
        var httpOrigin = apiUri.GetLeftPart(UriPartial.Authority);
        var wsScheme = apiUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";
        var wsOrigin = $"{wsScheme}://{apiUri.Authority}";
        _apiConnectSources = $"{httpOrigin} {wsOrigin}";
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var (ctx, apiConnectSources) = ((HttpContext, string))state;
            var headers = ctx.Response.Headers;

            // Prevent MIME-type sniffing.
            headers["X-Content-Type-Options"] = "nosniff";

            // Disallow framing (clickjacking protection). CSP frame-ancestors below
            // is the modern equivalent but X-Frame-Options is still widely checked.
            headers["X-Frame-Options"] = "DENY";

            // Legacy header; OWASP guidance is to disable the buggy legacy auditor
            // with "0" and rely on CSP instead.
            headers["X-XSS-Protection"] = "0";

            // Limit referrer leakage.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict powerful browser features by default.
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // HSTS only makes sense over HTTPS (UseHsts also sets this in non-dev;
            // setting it here is harmless/idempotent and covers every response).
            if (ctx.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Content-Security-Policy tuned for the server-rendered MVC app.
            //   • 'self' for scripts/styles/fonts/images plus the bundles in wwwroot.
            //   • script-src 'unsafe-inline' + the jQuery/validation CDNs: the shipped
            //     Webestica template, the SSR views (e.g. _AdminLayout) and the shared
            //     destructive-confirm modal (_ConfirmModal) rely on inline <script> blocks,
            //     and the validation partial loads jquery / jquery-validation from
            //     code.jquery.com + cdn.jsdelivr.net. Dropping these breaks form validation
            //     and the F8 confirm modal. (style-src keeps 'unsafe-inline' for the same
            //     inline-style reason as the template below.)
            //   • data: + https: images: avatars, map tiles, attachment proxy.
            //   • frame-ancestors 'none' = clickjacking protection (matches X-Frame-Options).
            //   • base-uri 'self', form-action 'self' = limit base-tag / form hijacking.
            //   • script-src also allows Google reCAPTCHA v3 (www.google.com + www.gstatic.com),
            //     loaded by the shared auth _RecaptchaField partial; reCAPTCHA renders a hidden
            //     challenge iframe from www.google.com, so frame-src must allow it too.
            const string scriptCdns =
                "https://code.jquery.com https://cdn.jsdelivr.net https://www.google.com https://www.gstatic.com";
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "img-src 'self' data: https:; " +
                $"script-src 'self' 'unsafe-inline' {scriptCdns}; " +
                // Fonts are self-hosted (V3/A5) — no Google Fonts stylesheet; 'unsafe-inline' kept for the template's inline styles.
                "style-src 'self' 'unsafe-inline'; " +
                // Self-hosted font files + base64 data: fonts (no fonts.gstatic.com — fonts are self-hosted).
                "font-src 'self' data:; " +
                // SignalR live-slots (§2.5): allow the cross-origin API host + its wss:// origin.
                $"connect-src 'self' {apiConnectSources}; " +
                // reCAPTCHA challenge iframe.
                "frame-src https://www.google.com; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                // form-action must allowlist the external OAuth authorization
                // endpoints: the provider sign-in buttons are <form method="post">,
                // so the challenge 302 to accounts.google.com / www.facebook.com is a
                // form-initiated cross-origin navigation. A 'self'-only form-action
                // makes Chromium refuse to follow that redirect, silently breaking
                // Google/Facebook external login.
                "form-action 'self' https://accounts.google.com https://www.facebook.com";

            // Observability (rule ERR4): surface the trace id so clients can
            // correlate a response with server-side logs/traces.
            var traceId = Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
            headers["X-Correlation-ID"] = traceId;

            return Task.CompletedTask;
        }, (context, _apiConnectSources));

        return _next(context);
    }
}
