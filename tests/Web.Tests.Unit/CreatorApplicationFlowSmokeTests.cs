using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
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

public sealed class CreatorApplicationFlowSmokeTests
{
    private const string Read = "Permission.Creator.Read";
    private const string Submit = "Permission.Creator.Submit";
    private const string Update = "Permission.Creator.Update";
    private const string Redeem = "Permission.Creator.RedeemInvitation";

    [Fact]
    public async Task Get_NoApplication_RendersCreateForm()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read, Submit]);

        var resp = await client.GetAsync("/creator/application");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Save draft", "first-time applicants get a create form");
        html.Should().Contain("Creator Application");
    }

    [Fact]
    public async Task Get_DraftApplication_RendersEditableFormWithSubmit()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor(
            [Read, Submit, Update],
            applicationJson: DraftJson());

        var resp = await client.GetAsync("/creator/application");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("Save changes", "a Draft is editable");
        html.Should().Contain("Submit for review", "a Draft can be submitted");
    }

    [Fact]
    public async Task Get_PendingApplication_RendersReadOnlyStatus()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read, Submit], applicationJson: PendingJson());

        var resp = await client.GetAsync("/creator/application");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("under review");
        html.Should().NotContain("Save draft");
        html.Should().NotContain("Save changes");
    }

    [Fact]
    public async Task Create_Succeeds_WithOnlyCreatorSubmit_AndRedirects()
    {
        using var f = new AppFactory();
        // Note: NO Creator.Update — proves Create is gated by Submit, not Update.
        var client = f.CreateClientFor([Read, Submit], postStatus: 201,
            postBody: """{ "applicationId": "33333333-3333-3333-3333-333333333333" }""");

        var resp = await PostFormAsync(client, "/creator/application/create",
            new() { ["Bio"] = "hello" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/application");
    }

    [Fact]
    public async Task Create_Conflict_ReturnsToFormWithError()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read, Submit], postStatus: 409);

        var resp = await PostFormAsync(client, "/creator/application/create",
            new() { ["Bio"] = "hello" });

        // Facade maps non-success to a re-render of the form (200), not a redirect.
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.Should().Contain("active creator application", "409 maps to a friendly conflict message");
    }

    [Fact]
    public async Task Update_Without_CreatorUpdate_Is403()
    {
        using var f = new AppFactory();
        // Has Submit but NOT Update → the Update route must forbid.
        var client = f.CreateClientFor([Read, Submit]);

        var resp = await PostFormAsync(client, "/creator/application/update",
            new()
            {
                ["ApplicationId"] = "44444444-4444-4444-4444-444444444444",
                ["Status"] = "Draft",
                ["Bio"] = "edit",
            });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Update is gated by Creator.Update; a user with only Submit must be forbidden");
    }

    [Fact]
    public async Task Create_Without_CreatorSubmit_Is403()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read]); // no Submit

        var resp = await PostFormAsync(client, "/creator/application/create",
            new() { ["Bio"] = "x" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Redeem_Without_Permission_Is403()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read, Submit]); // no RedeemInvitation

        var resp = await client.GetAsync("/creator/application/invite");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Redeem_Succeeds_WithPermission_AndRedirects()
    {
        using var f = new AppFactory();
        var client = f.CreateClientFor([Read, Submit, Redeem], postStatus: 200);

        var resp = await PostFormAsync(client, "/creator/application/invite",
            new() { ["Token"] = "valid-token" });

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("/creator/application");
    }

    [Fact]
    public async Task Sidebar_ApplicationLink_VisibleOnlyWithCreatorSubmit()
    {
        using var f = new AppFactory();

        // The sidebar Application nav-link carries a unique icon marker; assert on it
        // rather than the bare URL (the dashboard CTA also links to /creator/application).
        const string sidebarMarker = "bi-pencil-square fa-fw me-1";

        var withSubmit = await (f.CreateClientFor([Read, Submit]))
            .GetAsync("/creator/dashboard");
        (await withSubmit.Content.ReadAsStringAsync())
            .Should().Contain(sidebarMarker, "the Application sidebar link needs Creator.Submit");

        var withoutSubmit = await (f.CreateClientFor([Read]))
            .GetAsync("/creator/dashboard");
        (await withoutSubmit.Content.ReadAsStringAsync())
            .Should().NotContain(sidebarMarker, "no Application sidebar link without Creator.Submit");
    }

    // ── JSON fixtures ───────────────────────────────────────────────────────────

    private static string DraftJson() => """
        {
          "id": "55555555-5555-5555-5555-555555555555",
          "applicantUserId": "11111111-1111-1111-1111-111111111111",
          "status": "Draft",
          "bio": "draft bio",
          "portfolioUrls": [],
          "sampleWorkUrls": [],
          "nicheIds": [],
          "freeTags": [],
          "languageIds": [],
          "preferredRegionIds": [],
          "socialHandles": {},
          "reapplicationCount": 0,
          "createdAt": "2026-01-01T00:00:00Z"
        }
        """;

    private static string PendingJson() => DraftJson().Replace("\"Draft\"", "\"Pending\"");

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, content);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class AppFactory : WebApplicationFactory<Program>
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

                // Bypass antiforgery in the test host: HttpClient form posts don't carry
                // the CSRF token. Production behaviour is unchanged — this is test-only.
                services.RemoveAll<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoopAntiforgery>();
            });
        }

        public HttpClient CreateClientFor(
            string[] permissions,
            string? applicationJson = null,
            int postStatus = 200,
            string? postBody = null)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(st => st.Permissions = permissions);
                    s.Configure<TestApiState>(st =>
                    {
                        st.ApplicationMineJson = applicationJson;
                        st.PostStatus = postStatus;
                        st.PostBody = postBody;
                    });
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }).WithEnglishCulture();
        }
    }

    /// <summary>Test-only no-op antiforgery so HttpClient form posts pass validation.</summary>
    private sealed class NoopAntiforgery : Microsoft.AspNetCore.Antiforgery.IAntiforgery
    {
        private static readonly Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet Empty
            = new("t", "t", "f", "h");

        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetAndStoreTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) => Empty;
        public Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet GetTokens(Microsoft.AspNetCore.Http.HttpContext httpContext) => Empty;
        public Task<bool> IsRequestValidAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) => Task.FromResult(true);
        public Task ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpContext httpContext) => Task.CompletedTask;
        public void SetCookieTokenAndHeader(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }

    private sealed class TestApiState
    {
        public string? ApplicationMineJson { get; set; }
        public int PostStatus { get; set; } = 200;
        public string? PostBody { get; set; }
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "CreatorAppSmokeScheme";
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
                new Claim(ClaimTypes.Name, "Smoke Creator"),
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
            string? json = path switch
            {
                "/api/v1/blogs/creators/applications/mine" => _state.ApplicationMineJson,
                "/api/v1/blogs/creators/niches"            => "[]",
                _                                          => null,
            };

            if (string.IsNullOrWhiteSpace(json))
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));

            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
        {
            if (_state.PostStatus is >= 200 and < 300 && !string.IsNullOrWhiteSpace(_state.PostBody))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    _state.PostBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (data is not null)
                    return Task.FromResult(ApiResult<T>.Ok(data, _state.PostStatus));
            }

            // Non-success with NO error text — represents a bare status / unparsable
            // ProblemDetails, so the facade's friendly-message mapping is exercised.
            return Task.FromResult(_state.PostStatus is >= 200 and < 300
                ? ApiResult<T>.Fail(_state.PostStatus, "Empty response body.")
                : ApiResult<T>.Fail(_state.PostStatus, null));
        }

        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(_state.PostStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.PostStatus)
                : ApiResult.Fail(_state.PostStatus, $"stub {_state.PostStatus}"));

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(_state.PostStatus is >= 200 and < 300
                ? ApiResult.Ok(_state.PostStatus)
                : ApiResult.Fail(_state.PostStatus, $"stub {_state.PostStatus}"));

        // ── Unused ─────────────────────────────────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "n/a"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "n/a"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "n/a"));
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
