using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using YallaJo.Web.Infrastructure.Middleware;

namespace Web.Tests.Unit;

/// <summary>
/// Regression guard for external login: the provider sign-in buttons are
/// &lt;form method="post"&gt;, so the challenge 302 to the OAuth authorization
/// endpoint is a form-initiated cross-origin navigation. A 'self'-only
/// form-action makes Chromium refuse to follow that redirect, silently breaking
/// Google/Facebook external login. The CSP MUST allowlist both provider
/// authorization origins (while keeping 'self').
/// </summary>
public sealed class SecurityHeadersCspFormActionTests
{
    private static async Task<string> CaptureCspAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "https://localhost:57065",
            })
            .Build();

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, configuration);

        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        await middleware.InvokeAsync(context);

        // Headers are written via Response.OnStarting; trigger it explicitly.
        await context.Response.StartAsync();

        return context.Response.Headers["Content-Security-Policy"].ToString();
    }

    [Fact]
    public async Task Csp_FormAction_AllowsGoogleAndFacebookAuthorizationEndpoints()
    {
        var csp = await CaptureCspAsync();

        csp.Should().Contain("form-action 'self'");
        csp.Should().Contain("https://accounts.google.com");
        csp.Should().Contain("https://www.facebook.com");
    }
}
