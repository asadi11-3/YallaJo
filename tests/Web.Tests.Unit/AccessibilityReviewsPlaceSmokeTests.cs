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
/// FE-2D-1 — the Place detail page renders the accessibility-reviews section: the list
/// (one review) plus the write form for an authenticated user, and a sign-in CTA for
/// anonymous visitors. Tag helpers are processed (no raw markup).
/// </summary>
public sealed class AccessibilityReviewsPlaceSmokeTests
{
    private const string PlaceJson = """
        {"id":"33333333-3333-3333-3333-333333333333","name":"Petra Visitor Center","slug":"petra","placeType":"Landmark",
         "latitude":30.3,"longitude":35.4,"description":"d","averageRating":4.5,"reviewCount":2,"isVerified":true}
        """;

    private const string AccessibilityReviewsJson = """
        {"items":[{"id":"11111111-1111-1111-1111-111111111111","userId":"99999999-9999-9999-9999-999999999999",
          "targetType":1,"targetId":"33333333-3333-3333-3333-333333333333","rating":5,"title":"Step-free",
          "content":"Wide ramps and accessible restrooms.","visitDate":null,"featureTypesCsv":"Wheelchair,Mobility",
          "status":0,"createdAt":"2026-02-01T00:00:00Z","lastEditedAt":null}],"page":1,"pageSize":10,"totalCount":1}
        """;

    [Fact]
    public async Task PlaceDetail_Anonymous_RendersAccessibilitySection_WithSignInCta()
    {
        using var factory = new PlaceWebFactory();
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/places/petra");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Accessibility reviews");
        html.Should().Contain("Wide ramps and accessible restrooms.");
        html.Should().Contain("Sign in to write an accessibility review");
        html.Should().Contain("Reviewer 999999", "the author handle must be privacy-safe");
        html.Should().NotContain("99999999-9999-9999-9999-999999999999", "raw user GUID must not be exposed");
        html.Should().NotContain("asp-action");
    }

    [Fact]
    public async Task PlaceDetail_Authenticated_RendersWriteForm()
    {
        using var factory = new PlaceWebFactory();
        var client = factory.CreateClientFor(["Permission.AccessibilityReview.Create"]);

        var response = await client.GetAsync("/places/petra");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Write an accessibility review");
        html.Should().Contain("Submit accessibility review");
        html.Should().Contain("/places/petra/accessibility-reviews");
        html.Should().Contain("Wheelchair", "the feature-type checklist must render");
        html.Should().NotContain("Sign in to write an accessibility review");
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class PlaceWebFactory : WebApplicationFactory<Program>
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
                s.Configure<TestAuthState>(st => { st.Permissions = permissions; st.Anonymous = false; })));
            return f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public HttpClient CreateAnonymousClient()
        {
            var f = WithWebHostBuilder(b => b.ConfigureServices(s =>
                s.Configure<TestAuthState>(st => st.Anonymous = true)));
            return f.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; public bool Anonymous { get; set; } }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AccessibilityReviewSmokeScheme";
        private readonly TestAuthState _state;
        public TestAuthHandler(IOptionsMonitor<TestAuthSchemeOptions> o, ILoggerFactory l, UrlEncoder e, IOptions<TestAuthState> s)
            : base(o, l, e) => _state = s.Value;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_state.Anonymous) return Task.FromResult(AuthenticateResult.NoResult());

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
            string? json = null;
            if (path.StartsWith("/api/v1/social/accessibility/reviews", StringComparison.Ordinal))
                json = AccessibilityReviewsJson;
            else if (path.Contains("/api/v1/places/petra", StringComparison.Ordinal) && !path.Contains("/images") && !path.Contains("/accessibility"))
                json = PlaceJson;

            if (json is null)
            {
                // Tolerant facades treat a failure as empty — keep the page rendering.
                return Task.FromResult(ApiResult<T>.Fail(204, "no content"));
            }

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
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream s, string f, string c, IReadOnlyDictionary<string, string>? ff = null, string fn = "file", CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream s, string f, string c, IReadOnlyDictionary<string, string>? ff = null, string fn = "file", CancellationToken ct = default) => Task.FromResult(ApiResult<T>.Fail(501));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default) => Task.FromResult(ApiResult.Fail(501));
    }
}
