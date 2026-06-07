using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
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
/// FE-1D — authenticated runtime smoke for the Privacy &amp; Data page.
///
/// Boots the real YallaJo.Web host with <see cref="WebApplicationFactory{T}"/>, swaps
/// cookie auth for a deterministic test scheme (permissions in an access_token JWT) and
/// replaces <see cref="IApiClient"/> with an in-memory stub. Verifies anonymous challenge,
/// the Preference.Read gate, that the page renders (sidebar link, cross-link to Delete
/// Profile, no raw tag helpers), and that the export streams a JSON file attachment.
/// </summary>
public sealed class PrivacyPagesSmokeTests
{
    private const string UserId = "11111111-1111-1111-1111-111111111111";

    private const string ExportJson =
        """{ "userId": "11111111-1111-1111-1111-111111111111", "interactions": [], "preferences": null, "excludedEntities": [], "exportedAt": "2026-01-01T00:00:00Z" }""";

    [Fact]
    public async Task AnonymousUser_IsChallenged_NotServedPrivacyPage()
    {
        using var factory = new PrivacyWebFactory();
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/accounts/privacy");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutPreferenceRead_IsForbidden()
    {
        using var factory = new PrivacyWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"]);

        var response = await client.GetAsync("/accounts/privacy");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedReader_GetsPrivacyPage_WithSidebarLink_AndDeleteProfileCrossLink()
    {
        using var factory = new PrivacyWebFactory();
        var client = factory.CreateClientFor(["Permission.Preference.Read"]);

        var response = await client.GetAsync("/accounts/privacy");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "a Preference.Read holder must reach the privacy page (no 404/500)");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Privacy &amp; Data", "the privacy heading must render");
        html.Should().Contain("/accounts/privacy", "the sidebar Privacy link must render");
        html.Should().Contain("/accounts/delete", "a cross-link to the account-deletion page must be present");
        html.Should().Contain("DELETE", "the typed-confirmation prompt must be present");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task Export_StreamsJsonFileAttachment_WithExpectedFilename()
    {
        using var factory = new PrivacyWebFactory();
        var client = factory.CreateClientFor(["Permission.Preference.Read"], exportJson: ExportJson);

        var response = await client.GetAsync("/accounts/privacy/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Content.Headers.ContentDisposition.FileName.Should().Be("my-data-export.json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"userId\"", "the proxied export payload must be returned verbatim");
    }

    // ── Test host ──────────────────────────────────────────────────────────────

    internal sealed class PrivacyWebFactory : WebApplicationFactory<Program>
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

        public HttpClient CreateClientFor(string[] permissions, string? exportJson = null)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state =>
                    {
                        state.Authenticated = true;
                        state.Permissions = permissions;
                    });
                    s.Configure<TestApiState>(state => state.ExportJson = exportJson);
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public HttpClient CreateAnonymousClient()
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s => s.Configure<TestAuthState>(state => state.Authenticated = false)));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState
    {
        public bool Authenticated { get; set; } = true;
        public string[] Permissions { get; set; } = [];
    }

    private sealed class TestApiState
    {
        public string? ExportJson { get; set; }
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
    }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "PrivacySmokeTestScheme";

        private readonly TestAuthState _state;

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<TestAuthState> state)
            : base(options, logger, encoder)
            => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!_state.Authenticated)
            {
                return Task.FromResult(AuthenticateResult.Fail("anonymous"));
            }

            var jwt = new JwtSecurityToken(
                claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", UserId),
                new Claim(ClaimTypes.Name, "Privacy Tester"),
                new Claim("access_token", accessToken),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// In-memory <see cref="IApiClient"/> stub. Returns a canned export file for the
    /// export path; everything else (incl. the profile read for the sidebar) degrades
    /// to an "absent" 200 which the controller handles gracefully.
    /// </summary>
    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;

        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
        {
            if (path.Contains("/me/export", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(_state.ExportJson))
            {
                var bytes = Encoding.UTF8.GetBytes(_state.ExportJson);
                return Task.FromResult(ApiResult<ApiFile>.Ok(new ApiFile(bytes, "application/json", "export.json"), 200));
            }

            return Task.FromResult(ApiResult<ApiFile>.Fail(404, "not found"));
        }

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
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
            => Task.FromResult(ApiResult.Ok());
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
    }
}
