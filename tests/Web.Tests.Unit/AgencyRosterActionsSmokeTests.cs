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
/// FE-2C-3 — verifies the roster action buttons (Approve / Reject / Remove) are
/// permission-gated: rendered only when the caller holds the matching AgencyRoster
/// permission, with a non-empty roster (one guide + one pending application).
/// </summary>
public sealed class AgencyRosterActionsSmokeTests
{
    private const string GuidesJson = """
        [{"affiliationId":"a1111111-1111-1111-1111-111111111111","guideUserId":"b1111111-1111-1111-1111-111111111111","commissionPercentage":20,"joinedAt":"2026-01-01T00:00:00Z"}]
        """;
    private const string ApplicationsJson = """
        [{"id":"c1111111-1111-1111-1111-111111111111","guideUserId":"d1111111-1111-1111-1111-111111111111","agencyUserId":"e1111111-1111-1111-1111-111111111111","message":"hi","status":0,"rejectionReason":null,"reviewedAt":null,"createdAt":"2026-01-02T00:00:00Z"}]
        """;

    [Fact]
    public async Task WithAllActionPermissions_ShowsApproveRejectRemove()
    {
        using var factory = new ActionsWebFactory();
        var client = factory.CreateClientFor([
            "Permission.AgencyRoster.Read",
            "Permission.AgencyRoster.Approve",
            "Permission.AgencyRoster.Reject",
            "Permission.AgencyRoster.Delete",
        ]);

        var html = await (await client.GetAsync("/guide/agency/roster")).Content.ReadAsStringAsync();

        html.Should().Contain("Approve");
        html.Should().Contain("Reject");
        html.Should().Contain("Remove");
        html.Should().Contain("/applications/c1111111-1111-1111-1111-111111111111/approve");
        html.Should().Contain("/guides/b1111111-1111-1111-1111-111111111111/remove");
        html.Should().Contain("cannot be undone", "the remove confirmation copy must be present");
        html.Should().NotContain("<permission", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task WithoutActionPermissions_HidesActionButtons()
    {
        using var factory = new ActionsWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read"]);

        var html = await (await client.GetAsync("/guide/agency/roster")).Content.ReadAsStringAsync();

        html.Should().NotContain("/approve");
        html.Should().NotContain("/remove");
        html.Should().NotContain("/reject");
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ActionsWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets(); // Patch 0A.2: boot host without ambient user-secrets

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
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s => s.Configure<TestAuthState>(state => state.Permissions = permissions)));
            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AgencyRosterActionsSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<TestAuthState> state)
            : base(options, logger, encoder) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var claims = new[]
            {
                new Claim("sub", "99999999-9999-9999-9999-999999999999"),
                new Claim(ClaimTypes.Name, "Smoke Agency"),
                new Claim("access_token", accessToken),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            string json = path switch
            {
                "/api/v1/agency/guides" => GuidesJson,
                "/api/v1/agency/applications" => ApplicationsJson,
                _ => "[]", // invitations/sent + available
            };
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
    }
}
