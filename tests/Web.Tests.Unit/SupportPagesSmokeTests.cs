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
/// FE-1C — authenticated runtime smoke for the user Support Tickets pages.
///
/// Boots the real YallaJo.Web host with <see cref="WebApplicationFactory{T}"/>, swaps
/// cookie auth for a deterministic test scheme (permissions encoded in an access_token
/// JWT, exactly how production CurrentUser resolves them) and replaces
/// <see cref="IApiClient"/> with an in-memory stub returning canned /api/v1/support reads.
///
/// Verifies:
///   * anonymous GET /accounts/support challenges (does not 200),
///   * an authenticated SupportTicket.Read holder gets 200 on list + detail (no 404/500),
///   * the Support sidebar link renders,
///   * the "New ticket" CTA links to /contact (creation reuses the Contact form),
///   * internal staff notes are NOT present in the rendered detail HTML,
///   * tag helpers are processed (no raw asp-* / &lt;permission&gt; emitted).
/// </summary>
public sealed class SupportPagesSmokeTests
{
    private const string OwnerId = "11111111-1111-1111-1111-111111111111";
    private const string TicketId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    private static readonly string ListJson = $$"""
    {
      "items": [
        { "id": "{{TicketId}}", "createdByUserId": "{{OwnerId}}", "category": "BookingIssue", "subject": "My tour was cancelled", "priority": "Normal", "status": "Open", "createdAt": "2026-01-01T10:00:00Z", "rowVersion": "Zm9v" }
      ],
      "nextCursor": null
    }
    """;

    private static readonly string DetailJson = $$"""
    {
      "id": "{{TicketId}}", "createdByUserId": "{{OwnerId}}", "category": "BookingIssue",
      "subject": "My tour was cancelled", "priority": "Normal", "status": "Open",
      "createdAt": "2026-01-01T10:00:00Z", "rowVersion": "Zm9v",
      "messages": [
        { "id": "cccccccc-cccc-cccc-cccc-cccccccccccc", "authorUserId": "{{OwnerId}}", "body": "Customer visible message", "isInternal": false, "createdAt": "2026-01-01T11:00:00Z" },
        { "id": "dddddddd-dddd-dddd-dddd-dddddddddddd", "authorUserId": "22222222-2222-2222-2222-222222222222", "body": "SECRET_INTERNAL_NOTE", "isInternal": true, "createdAt": "2026-01-01T11:30:00Z" }
      ]
    }
    """;

    // ── Anonymous ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnonymousUser_IsChallenged_NotServedSupportList()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/accounts/support");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK, "anonymous users must not see the support inbox");
    }

    // ── Permission gate ────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticatedUser_WithoutSupportRead_IsForbidden()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"]);

        var response = await client.GetAsync("/accounts/support");

        // RequirePermission(SupportTicket.Read) → ForbidResult when the permission is absent.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ── Happy path: list ───────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticatedReader_GetsSupportList_WithSidebarLink_AndNewTicketCtaToContact()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateClientFor(["Permission.SupportTicket.Read"], listJson: ListJson);

        var response = await client.GetAsync("/accounts/support");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "a SupportTicket.Read holder must reach the support inbox (no post-auth 404/500)");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("My Support Tickets", "the support list heading must render");
        html.Should().Contain("/accounts/support", "the sidebar Support link must render");
        html.Should().Contain("/contact", "the 'New ticket' CTA must link to the existing Contact form");
        html.Should().Contain("My tour was cancelled", "the seeded ticket subject must render");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
        html.Should().NotContain("<permission", "the permission tag helper must be processed, not emitted raw");
    }

    // ── Happy path: detail hides internal notes ────────────────────────────────

    [Fact]
    public async Task AuthenticatedReader_GetsTicketDetail_WithoutInternalNotes()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateClientFor(["Permission.SupportTicket.Read"], detailJson: DetailJson);

        var response = await client.GetAsync($"/accounts/support/{TicketId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the owner must reach their ticket detail (no 404/500)");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Customer visible message", "the owner's own message must render");
        html.Should().NotContain("SECRET_INTERNAL_NOTE", "internal staff notes must never be shown to the owner");
    }

    [Fact]
    public async Task TicketDetail_WithClosePermission_RendersCloseForm_WithRowVersion()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateClientFor(
            ["Permission.SupportTicket.Read", "Permission.SupportTicket.Close"], detailJson: DetailJson);

        var response = await client.GetAsync($"/accounts/support/{TicketId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Close ticket", "a Close-permission holder must see the close action on an open ticket");
        html.Should().Contain("Zm9v", "the RowVersion must be embedded in the close form for optimistic concurrency");
    }

    [Fact]
    public async Task TicketDetail_WithoutClosePermission_HidesCloseForm()
    {
        using var factory = new SupportWebFactory();
        var client = factory.CreateClientFor(["Permission.SupportTicket.Read"], detailJson: DetailJson);

        var response = await client.GetAsync($"/accounts/support/{TicketId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        // The <permission require="SupportTicket.Close"> gate (now active via the Accounts
        // _ViewImports) must suppress the close form for a Read-only user.
        html.Should().NotContain("Close ticket",
            "a user without SupportTicket.Close must not see the close action");
    }

    // ── Test host ──────────────────────────────────────────────────────────────

    internal sealed class SupportWebFactory : WebApplicationFactory<Program>
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

        public HttpClient CreateClientFor(string[] permissions, string? listJson = null, string? detailJson = null)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state =>
                    {
                        state.Authenticated = true;
                        state.Permissions = permissions;
                    });
                    s.Configure<TestApiState>(state =>
                    {
                        state.SupportListJson = listJson;
                        state.SupportDetailJson = detailJson;
                    });
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
        public string? SupportListJson { get; set; }
        public string? SupportDetailJson { get; set; }
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
    }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "SupportSmokeTestScheme";

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
                new Claim("sub", OwnerId),
                new Claim(ClaimTypes.Name, "Support Tester"),
                new Claim("access_token", accessToken),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// In-memory <see cref="IApiClient"/> stub for the support reads + the profile read
    /// (used by the controller's sidebar population). Anything else returns an "absent" 200.
    /// </summary>
    private sealed class StubApiClient : IApiClient
    {
        private readonly TestApiState _state;

        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            string? json = null;
            if (path.StartsWith("/api/v1/support/tickets/", StringComparison.OrdinalIgnoreCase))
            {
                json = _state.SupportDetailJson;
            }
            else if (path.StartsWith("/api/v1/support/tickets", StringComparison.OrdinalIgnoreCase))
            {
                json = _state.SupportListJson;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                // Mirror the real ApiClient "absent" semantics so facades degrade gracefully.
                return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
            }

            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Task.FromResult(
                data is null ? ApiResult<T>.Fail(200, "Could not parse response body.") : ApiResult<T>.Ok(data, 200));
        }

        // ── Unused by the support read path ───────────────────────────────────
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
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
