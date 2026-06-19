using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Authenticated runtime smoke for the Creator dashboard (CCD-0 shell + CCD-1 gate).
///
/// Boots the real YallaJo.Web host with <see cref="WebApplicationFactory{T}"/>,
/// replaces cookie auth with a deterministic test scheme (permissions encoded in an
/// access_token JWT — exactly how production <c>CurrentUser</c> resolves them), and
/// replaces <see cref="IApiClient"/> with an in-memory stub so the dashboard's
/// /profile/mine and /applications/mine reads return canned states without a live API.
///
/// Verifies, for an authenticated user:
///   * GET /creator/dashboard resolves with HTTP 200 (no post-auth 404/500),
///   * the Creator sidebar renders,
///   * the Dashboard nav link renders ONLY when Permission.Creator.Read is present
///     (the &lt;permission&gt; tag-helper gate),
///   * NotApplied state shows the "Become a creator" CTA and no stats,
///   * Approved state shows real stats from the profile read.
/// </summary>
public sealed class CreatorDashboardAuthenticatedSmokeTests
{
    [Fact]
    public async Task AuthenticatedUser_WithCreatorRead_GetsDashboard_AndSeesSidebarLink()
    {
        using var factory = new CreatorWebFactory();
        var client = factory.CreateClientFor(permissions: ["Permission.Creator.Read"]);

        var response = await client.GetAsync("/creator/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "an authenticated user must reach the Creator dashboard");

        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("creatorDashboardMenu", "the Creator sidebar partial must render");
        html.Should().Contain("Creator Dashboard", "the dashboard heading must render");
        // Assert on the rendered anchor href (the sidebar Dashboard link), NOT a bare URL
        // substring — the layout's language-switcher form emits the current URL as a hidden
        // returnUrl input (`<input value="/creator/dashboard">`), which would match either way
        // and silently mask gating regressions. `href="..."` only matches real anchors.
        html.Should().Contain("href=\"/creator/dashboard\"", "the permission-gated Dashboard link must render for Creator.Read holders");
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutCreatorRead_GetsDashboard_ButSidebarLinkIsHidden()
    {
        using var factory = new CreatorWebFactory();
        var client = factory.CreateClientFor(permissions: ["Permission.SomethingElse.Read"]);

        var response = await client.GetAsync("/creator/dashboard");

        // [Authorize]-only controller, so the page still loads; only the link is gated.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("creatorDashboardMenu", "the sidebar chrome still renders");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
        // The bare-string assertion `NotContain("/creator/dashboard")` was brittle: the
        // layout's language-switcher form emits the current request URL as a hidden
        // returnUrl input value (see _Navbar.cshtml ~line 215), which always matched.
        // Restrict the assertion to the rendered anchor's href so it really verifies the
        // <permission>-gated sidebar Dashboard link is absent.
        html.Should().NotContain("href=\"/creator/dashboard\"",
            "the Dashboard nav link must be hidden when Creator.Read is absent");
    }

    [Fact]
    public async Task NotApplied_State_ShowsBecomeACreatorCta_AndNoStats()
    {
        // Both /mine reads absent (stub default) → NotApplied.
        using var factory = new CreatorWebFactory();
        var client = factory.CreateClientFor(permissions: ["Permission.Creator.Read"]);

        var response = await client.GetAsync("/creator/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Become a creator", "a user who has never applied must see the application CTA");
        // CCD-2: the application CTA is now a REAL link to the Creator application page
        // (it was a disabled 'Coming soon' button in CCD-0/CCD-1).
        html.Should().Contain("/creator/application", "the application CTA now links to the real page");
        html.Should().NotContain("Total Views", "no stats cards may render outside the approved state");
    }

    [Fact]
    public async Task Approved_State_ShowsRealStats()
    {
        using var factory = new CreatorWebFactory();
        var client = factory.CreateClientFor(
            permissions: ["Permission.Creator.Read"],
            profileJson: """
                {
                  "id": "22222222-2222-2222-2222-222222222222",
                  "userId": "11111111-1111-1111-1111-111111111111",
                  "slug": "smoke-creator",
                  "displayName": "Smoke Creator",
                  "status": "Active",
                  "trustTier": "Trusted",
                  "articleCount": 7,
                  "totalViewCount": 1234,
                  "totalReactionCount": 56,
                  "totalCommentCount": 12,
                  "followerCount": 89
                }
                """,
            // my-blogs returns an empty list (snapshot is supplementary).
            myBlogsJson: "[]");

        var response = await client.GetAsync("/creator/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Active creator", "the approved badge must render for an Active profile");
        html.Should().Contain("Total Views", "the stats cards must render for an approved creator");
        html.Should().Contain("1234", "real backend view count must be displayed");
        html.Should().Contain("Recent Articles");
        html.Should().NotContain("Become a creator", "approved creators must not see the application CTA");
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class CreatorWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets(); // Patch 0A.2: boot host without ambient user-secrets

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                });

                // Replace the real typed HTTP client with an in-memory stub so the
                // dashboard's /mine + /my-blogs reads are deterministic (no live API).
                services.RemoveAll<IApiClient>();
                services.AddScoped<IApiClient, StubApiClient>();
            });
        }

        public HttpClient CreateClientFor(
            string[] permissions,
            string? profileJson = null,
            string? applicationJson = null,
            string? myBlogsJson = null)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state => state.Permissions = permissions);
                    s.Configure<TestApiState>(state =>
                    {
                        state.ProfileMineJson = profileJson;
                        state.ApplicationMineJson = applicationJson;
                        state.MyBlogsJson = myBlogsJson;
                    });
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }).WithEnglishCulture();
        }
    }

    private sealed class TestAuthState
    {
        public string[] Permissions { get; set; } = [];
    }

    private sealed class TestApiState
    {
        public string? ProfileMineJson { get; set; }
        public string? ApplicationMineJson { get; set; }
        public string? MyBlogsJson { get; set; }
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
    }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorSmokeTestScheme";

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
            var jwt = new JwtSecurityToken(
                claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "11111111-1111-1111-1111-111111111111"),
                new Claim(ClaimTypes.Name, "Smoke Creator"),
                new Claim("access_token", accessToken),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// In-memory <see cref="IApiClient"/> stub. Returns canned bodies for the three
    /// dashboard reads; a null body is surfaced exactly as the real ApiClient does for
    /// a 200-with-empty-body ("absent") so the facade's null-handling is exercised.
    /// </summary>
    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;

        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            string? json = path switch
            {
                "/api/v1/blogs/creators/profile/mine"      => _state.ProfileMineJson,
                "/api/v1/blogs/creators/applications/mine" => _state.ApplicationMineJson,
                "/api/v1/blogs/my-blogs"                   => _state.MyBlogsJson,
                _                                          => null,
            };

            if (string.IsNullOrWhiteSpace(json))
            {
                // Mirror the real ApiClient: 200 with empty body → 200 failure ("absent").
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
            }

            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Task.FromResult(
                data is null
                    ? ApiResult<T>.Fail(200, "Could not parse response body.")
                    : ApiResult<T>.Ok(data, 200));
        }

        // ── Unused by the dashboard read path ─────────────────────────────────
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
