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
/// FE-2C-1 — authenticated runtime smoke for /guide/agency/roster: the page loads with
/// AgencyRoster.Read (empty states render + sidebar link), and is forbidden without it.
/// </summary>
public sealed class AgencyRosterSmokeTests
{
    [Fact]
    public async Task WithAgencyRosterRead_PageLoads_ShowsEmptyStates_AndSidebarLink()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read"]);

        var response = await client.GetAsync("/guide/agency/roster");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("Agency Roster");
        html.Should().Contain("No guides on your roster yet");
        html.Should().Contain("No applications yet");
        html.Should().Contain("No invitations sent yet");
        html.Should().Contain("/guide/agency/roster", "the gated sidebar link must render for AgencyRoster.Read holders");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task WithoutAgencyRosterRead_IsForbidden()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"]);

        var response = await client.GetAsync("/guide/agency/roster");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RosterIndex_WithCreate_ShowsInviteCta()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read", "Permission.AgencyRoster.Create"]);

        var html = await (await client.GetAsync("/guide/agency/roster")).Content.ReadAsStringAsync();

        html.Should().Contain("Invite a guide", "the gated Invite CTA must render with AgencyRoster.Create");
        html.Should().Contain("/guide/agency/roster/invite");
    }

    [Fact]
    public async Task RosterIndex_WithoutCreate_HidesInviteCta()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read"]);

        var html = await (await client.GetAsync("/guide/agency/roster")).Content.ReadAsStringAsync();

        html.Should().NotContain("/guide/agency/roster/invite",
            "the Invite CTA must be hidden without AgencyRoster.Create");
    }

    [Fact]
    public async Task InviteForm_WithCreate_Loads()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read", "Permission.AgencyRoster.Create"]);

        var response = await client.GetAsync("/guide/agency/roster/invite");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Invite a guide");
    }

    [Fact]
    public async Task InviteForm_WithoutCreate_IsForbidden()
    {
        using var factory = new AgencyRosterWebFactory();
        var client = factory.CreateClientFor(["Permission.AgencyRoster.Read"]);

        var response = await client.GetAsync("/guide/agency/roster/invite");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class AgencyRosterWebFactory : WebApplicationFactory<Program>
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
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s => s.Configure<TestAuthState>(state => state.Permissions = permissions)));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AgencyRosterSmokeScheme";
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
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    // All three roster reads return empty lists → empty-state render.
    private sealed class StubApiClient : IApiClient
    {
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path.StartsWith("/api/v1/agency/", StringComparison.Ordinal))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    "[]", new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Task.FromResult(data is null
                    ? ApiResult<T>.Fail(200, "Could not parse response body.")
                    : ApiResult<T>.Ok(data, 200));
            }

            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
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
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
    }
}
