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
/// Host-level smoke for the CCD-6 public-profile preview. Boots the real YallaJo.Web
/// host, injects a signed-in user, and stubs IApiClient so /profile/mine and the
/// public-by-slug endpoints are deterministic.
///
/// Covers: no-profile → redirect to Application; inactive/unavailable notice; Active
/// preview rendering of public data; public 404 → unavailable; sidebar link gate;
/// dashboard CTA in Approved state; and that NO internal fields appear in the HTML.
/// </summary>
public sealed class CreatorPreviewFlowSmokeTests
{
    private const string Read = "Permission.Creator.Read";

    // Sentinels seeded into the public-profile JSON; must NEVER appear in the HTML.
    private const string InternalUserId = "99999999-9999-9999-9999-999999999999";
    private const string InternalProviderId = "88888888-8888-8888-8888-888888888888";

    [Fact]
    public async Task NoProfile_RedirectsToApplication()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read]); // mine returns absent → NoProfile

        var resp = await client.GetAsync("/creator/preview");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/application");
    }

    [Fact]
    public async Task InactiveProfile_ShowsUnavailable()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read], mineJson: MineJson("Suspended"));

        var resp = await client.GetAsync("/creator/preview");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Public preview unavailable");
    }

    [Fact]
    public async Task ActiveProfile_RendersPublicPreview()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active"),
            publicProfileJson: PublicProfileJson(),
            blogsJson: OneBlogJson());

        var resp = await client.GetAsync("/creator/preview");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Jane Creator");
        html.Should().Contain("read-only preview");
        html.Should().Contain("Trusted", "the public trust tier badge renders");
        html.Should().Contain("Published Articles");
        html.Should().Contain("My Public Post", "a published article title renders");
        html.Should().Contain("/blog/post-1", "article titles link to the canonical public article route (Public/Blog/Post)");
    }

    [Fact]
    public async Task ActiveProfile_NoBlogs_ShowsEmptyPublishedState()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active"),
            publicProfileJson: PublicProfileJson(),
            blogsJson: EmptyBlogsJson());

        var html = await (await client.GetAsync("/creator/preview")).Content.ReadAsStringAsync();
        html.Should().Contain("No published articles yet");
    }

    [Fact]
    public async Task PublicProfile404_ShowsUnavailable()
    {
        using var f = new PreviewFactory();
        // mine says Active, but the public profile fetch 404s (edge: became inactive).
        var client = f.CreateClientFor([Read], mineJson: MineJson("Active"), publicProfileStatus: 404);

        var html = await (await client.GetAsync("/creator/preview")).Content.ReadAsStringAsync();
        html.Should().Contain("Public preview unavailable");
    }

    [Fact]
    public async Task ActivePreview_DoesNotRenderInternalFields()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read],
            mineJson: MineJson("Active"),
            publicProfileJson: PublicProfileJson(),
            blogsJson: OneBlogJson());

        var html = await (await client.GetAsync("/creator/preview")).Content.ReadAsStringAsync();

        html.Should().NotContain(InternalUserId, "the internal UserId must not be exposed");
        html.Should().NotContain(InternalProviderId, "LinkedProviderId must not be exposed");
        html.Should().NotContain("LinkedProviderId");
        html.Should().NotContain("\"Status\"");
    }

    [Fact]
    public async Task Sidebar_PublicPreviewLink_VisibleWithCreatorRead()
    {
        using var f = new PreviewFactory();
        var client = f.CreateClientFor([Read], mineJson: MineJson("Active"),
            publicProfileJson: PublicProfileJson(), blogsJson: EmptyBlogsJson());

        var html = await (await client.GetAsync("/creator/preview")).Content.ReadAsStringAsync();
        html.Should().Contain("fa-solid fa-eye fa-fw me-1", "the Public Preview sidebar link renders for Creator.Read");
    }

    [Fact]
    public async Task Dashboard_ShowsPreviewCta_InApprovedState()
    {
        using var f = new PreviewFactory();
        // Dashboard reads /profile/mine (Active) → Approved state → preview CTA shown.
        var client = f.CreateClientFor([Read], mineJson: MineJson("Active"));

        var html = await (await client.GetAsync("/creator/dashboard")).Content.ReadAsStringAsync();
        html.Should().Contain("Preview public profile", "the approved dashboard shows a real preview CTA");
        html.Should().Contain("/creator/preview");
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────

    private static string MineJson(string status) => $$"""
        { "id": "11111111-1111-1111-1111-111111111111", "userId": "{{InternalUserId}}",
          "slug": "jane-creator", "displayName": "Jane Creator", "status": "{{status}}",
          "trustTier": "Trusted", "articleCount": 7, "totalViewCount": 1234,
          "totalReactionCount": 56, "totalCommentCount": 12, "followerCount": 89 }
        """;

    private static string PublicProfileJson() => $$"""
        { "id": "11111111-1111-1111-1111-111111111111", "userId": "{{InternalUserId}}",
          "slug": "jane-creator", "displayName": "Jane Creator", "bio": "hello",
          "avatarUrl": null, "trustTier": "Trusted", "status": "Active",
          "articleCount": 7, "totalViewCount": 1234, "totalReactionCount": 56,
          "totalCommentCount": 12, "followerCount": 89, "linkedProviderId": "{{InternalProviderId}}",
          "createdAt": "2026-01-01T00:00:00Z" }
        """;

    private static string OneBlogJson() => """
        { "items": [ { "id": "55555555-5555-5555-5555-555555555555", "slug": "post-1",
          "title": "My Public Post", "summary": "a summary", "publishedAt": "2026-01-02T00:00:00Z",
          "viewCount": 10, "readTimeMinutes": 4, "languageCode": "en", "isFeatured": false } ],
          "pageNumber": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1,
          "hasPreviousPage": false, "hasNextPage": false }
        """;

    private static string EmptyBlogsJson() => """
        { "items": [], "pageNumber": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0,
          "hasPreviousPage": false, "hasNextPage": false }
        """;

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class PreviewFactory : WebApplicationFactory<Program>
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

        public HttpClient CreateClientFor(
            string[] permissions,
            string? mineJson = null,
            string? publicProfileJson = null,
            string? blogsJson = null,
            int publicProfileStatus = 200)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.Configure<TestApiState>(st =>
                    {
                        st.MineJson = mineJson;
                        st.PublicProfileJson = publicProfileJson;
                        st.BlogsJson = blogsJson;
                        st.PublicProfileStatus = publicProfileStatus;
                    });
                }));
            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }).WithEnglishCulture();
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }

    private sealed class TestApiState
    {
        public string? MineJson { get; set; }
        public string? PublicProfileJson { get; set; }
        public string? BlogsJson { get; set; }
        public int PublicProfileStatus { get; set; } = 200;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorPreviewSmokeScheme";
        private readonly TestAuthState _state;

        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l,
            UrlEncoder e, IOptions<TestAuthState> state) : base(o, l, e) => _state = state.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwt = new JwtSecurityToken(claims: _state.Permissions.Select(p => new Claim("Permission", p)));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "11111111-1111-1111-1111-111111111111"),
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
        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path.Contains("/creators/profiles/", StringComparison.Ordinal)
                && path.EndsWith("/blogs", StringComparison.Ordinal) is false
                && path.Contains("/blogs?", StringComparison.Ordinal) is false)
            {
                // public profile by slug
                if (_state.PublicProfileStatus == 404)
                    return Task.FromResult(ApiResult<T>.Fail(404, "Not found."));
                return Deserialize<T>(_state.PublicProfileJson);
            }
            if (path.Contains("/creators/profiles/", StringComparison.Ordinal)
                && (path.Contains("/blogs", StringComparison.Ordinal)))
            {
                return Deserialize<T>(_state.BlogsJson);
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
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
    }
}
