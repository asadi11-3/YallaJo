using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-2D-3 — /accounts/accessibility-reviews loads (200) with AccessibilityReview.Read,
/// shows the empty state, and is forbidden without the permission.
/// </summary>
public sealed class AccountsAccessibilityReviewsSmokeTests
{
    [Fact]
    public async Task WithRead_PageLoads_ShowsEmptyState_AndSidebarLink()
    {
        using var factory = new AccountsWebFactory();
        var client = factory.CreateClientFor(["Permission.AccessibilityReview.Read"]);

        var response = await client.GetAsync("/accounts/accessibility-reviews");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("My accessibility reviews");
        html.Should().Contain("haven't written any accessibility reviews");
        html.Should().Contain("/accounts/accessibility-reviews", "the sidebar link must render");
        html.Should().NotContain("asp-action");
    }

    [Fact]
    public async Task WithoutRead_IsForbidden()
    {
        using var factory = new AccountsWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"]);

        var response = await client.GetAsync("/accounts/accessibility-reviews");

        // [RequirePermission(AccessibilityReview.Read)] → Forbidden (authenticated, no perm).
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class AccountsWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                });
                services.RemoveAll<IApiClient>();
                services.AddScoped<IApiClient, StubApiClient>();
            });
        }

        public HttpClient CreateClientFor(string[] permissions)
        {
            var f = WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.Configure<TestAuthState>(st => st.Permissions = permissions)));
            return f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AccountsAccessibilityReviewSmokeScheme";
        private readonly TestAuthState _state;
        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l, UrlEncoder e, IOptions<TestAuthState> s)
            : base(o, l, e) => _state = s.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var claims = new[]
            {
                new Claim("sub", "88888888-8888-8888-8888-888888888888"),
                new Claim(ClaimTypes.Name, "Smoke User"),
                new Claim("access_token", accessToken),
            };
            var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), SchemeName)));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path.StartsWith("/api/v1/social/accessibility/reviews/my", StringComparison.Ordinal))
                return Deserialize<T>("""{"items":[],"nextCursor":null}""");
            if (path.StartsWith("/api/v1/accounts/profile", StringComparison.Ordinal) || path.Contains("/profile", StringComparison.Ordinal))
                return Deserialize<T>("""{"firstName":"Smoke","lastName":"User","email":"s@x.test","displayName":"Smoke User"}""");
            return Task.FromResult(ApiResult<T>.Fail(204, "no content"));
        }

        private static Task<ApiResult<T>> Deserialize<T>(string json)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null ? ApiResult<T>.Fail(200, "parse") : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default) => Task.FromResult(ApiResult<ApiFile>.Fail(501));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream s, string f, string c, string ff = "file", CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream s, string f, string c, IReadOnlyDictionary<string, string>? ff = null, string fn = "file", CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
    }
}
