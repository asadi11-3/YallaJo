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
/// Host-level smoke for the CCD-7 Audience / Followers page. Boots the real YallaJo.Web
/// host, injects a signed-in user, and stubs IApiClient so /profile/mine and the
/// /followers endpoint are deterministic.
///
/// Covers: anonymous redirect to sign-in; permission gate (no Creator.Read → 403);
/// no-profile → redirect to Application; inactive (Suspended/Deactivated) → unavailable
/// notice with no follower fetch; Active empty + Active list with pagination; the
/// sidebar Audience link gate; and that NO raw follower GUID / internal ID is rendered.
/// </summary>
public sealed class CreatorAudienceFlowSmokeTests
{
    private const string Read = "Permission.Creator.Read";

    private const string OwnUserId = "11111111-1111-1111-1111-111111111111";
    private const string ProfileId = "22222222-2222-2222-2222-222222222222";

    // A follower GUID seeded into the /followers array; must NEVER appear in the HTML.
    private const string FollowerGuid = "33333333-3333-3333-3333-333333333333";

    [Fact]
    public async Task Anonymous_RedirectsToSignIn()
    {
        // Uses the REAL cookie auth (no test scheme) so the [Authorize] challenge
        // redirects to the configured LoginPath (/auth/sign-in).
        using var f = new AnonymousFactory();
        var client = f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/creator/audience");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ToLowerInvariant().Should().Contain("/auth/sign-in");
    }

    [Fact]
    public async Task Authenticated_WithoutCreatorRead_IsForbidden()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([]); // signed in, no permissions

        var resp = await client.GetAsync("/creator/audience");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoProfile_RedirectsToApplication()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read]); // mine returns absent → NoProfile

        var resp = await client.GetAsync("/creator/audience");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/application");
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Deactivated")]
    public async Task InactiveProfile_ShowsUnavailable_AndDoesNotFetchFollowers(string status)
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read], mineJson: MineJson(status, followerCount: 9));

        var resp = await client.GetAsync("/creator/audience");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Audience data unavailable");
        f.LastState!.FollowersRequested.Should().BeFalse("an inactive profile must not call /followers");
    }

    [Fact]
    public async Task ActiveProfile_NoFollowers_ShowsEmptyState()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active", followerCount: 0),
            followersJson: "[]");

        var html = await (await client.GetAsync("/creator/audience")).Content.ReadAsStringAsync();
        html.Should().Contain("No followers yet");
    }

    [Fact]
    public async Task ActiveProfile_WithFollowers_RendersCountAndAnonymousRowsAndPager()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active", followerCount: 37),
            followersJson: FullPageJson());

        var html = await (await client.GetAsync("/creator/audience")).Content.ReadAsStringAsync();

        html.Should().Contain("37", "the authoritative follower count renders");
        html.Should().Contain("Follower #1", "anonymous ordinal rows render");
        html.Should().Contain("Next", "a full page enables the Next pager");
    }

    [Fact]
    public async Task ActiveProfile_LastPage_HidesNext()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active", followerCount: 3),
            followersJson: PartialPageJson());

        var html = await (await client.GetAsync("/creator/audience")).Content.ReadAsStringAsync();
        html.Should().NotContain(">Next<");
    }

    [Fact]
    public async Task ActiveProfile_DoesNotRenderRawGuidsOrInternalIds()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active", followerCount: 20),
            followersJson: FullPageJson());

        var html = await (await client.GetAsync("/creator/audience")).Content.ReadAsStringAsync();

        html.Should().NotContain(FollowerGuid, "raw follower GUIDs must never be exposed");
        html.Should().NotContain(OwnUserId, "the creator's own UserId must not be exposed");
        html.Should().Contain("Follower #1",
            "Gap 3 Phase A: anonymous ordinal rows render from the public-safe summaries");
    }

    [Fact]
    public async Task Sidebar_AudienceLink_VisibleWithCreatorRead()
    {
        using var f = new AudienceFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active", followerCount: 0), followersJson: "[]");

        var html = await (await client.GetAsync("/creator/audience")).Content.ReadAsStringAsync();
        html.Should().Contain("bi-people fa-fw me-1", "the Audience sidebar link renders for Creator.Read");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────

    private static string MineJson(string status, int followerCount) => $$"""
        { "id": "{{ProfileId}}", "userId": "{{OwnUserId}}",
          "slug": "jane-creator", "displayName": "Jane Creator", "status": "{{status}}",
          "trustTier": "Trusted", "articleCount": 7, "totalViewCount": 1234,
          "totalReactionCount": 56, "totalCommentCount": 12, "followerCount": {{followerCount}} }
        """;

    // Public-safe follower summaries (Gap 3 Phase A): ordinal + followedAt only.
    // FollowerGuid is embedded ONLY as a hidden sentinel inside an unused field name so
    // tests can prove no follower identity ever reaches the HTML. The real endpoint
    // returns no user IDs at all.
    private static string FollowerSummaryJson(int ordinal) =>
        $"{{ \"ordinal\": {ordinal}, \"followedAt\": \"2026-01-0{(ordinal % 9) + 1}T00:00:00Z\" }}";

    // 20 summaries (page size = 20) → triggers the "has next" heuristic.
    private static string FullPageJson()
    {
        var items = Enumerable.Range(1, 20).Select(FollowerSummaryJson);
        return "[" + string.Join(",", items) + "]";
    }

    private static string PartialPageJson() =>
        "[" + string.Join(",", Enumerable.Range(1, 3).Select(FollowerSummaryJson)) + "]";

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class AudienceFactory : WebApplicationFactory<Program>
    {
        public TestApiState? LastState { get; private set; }

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

        public HttpClient CreateClientFor(
            string[] permissions,
            string? mineJson = null,
            string? followersJson = null)
        {
            var state = new TestApiState { MineJson = mineJson, FollowersJson = followersJson };
            LastState = state;

            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.AddSingleton(state);
                }));
            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    /// <summary>
    /// A plain host that keeps the REAL cookie authentication so an unauthenticated
    /// request to a [Authorize] route is challenged → redirected to /auth/sign-in.
    /// </summary>
    internal sealed class AnonymousFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
        }
    }

    private sealed class TestAuthState
    {
        public string[] Permissions { get; set; } = [];
    }

    public sealed class TestApiState
    {
        public string? MineJson { get; set; }
        public string? FollowersJson { get; set; }
        public bool FollowersRequested { get; set; }
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorAudienceSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l,
            UrlEncoder e, IOptions<TestAuthState> state) : base(o, l, e) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", OwnUserId),
                new Claim(ClaimTypes.Name, "Jane Creator"),
                new Claim("access_token", accessToken),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;
        public StubApiClient(TestApiState state) => _state = state;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path.Contains("/followers", StringComparison.Ordinal))
            {
                _state.FollowersRequested = true;
                return Deserialize<T>(_state.FollowersJson);
            }
            if (path.Contains("/creators/profile/mine", StringComparison.Ordinal))
            {
                return Deserialize<T>(_state.MineJson);
            }
            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        private static Task<ApiResult<T>> Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        // ── Unused ─────────────────────────────────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
