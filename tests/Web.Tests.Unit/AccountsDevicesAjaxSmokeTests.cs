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
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Auth.Models.Sessions;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Accounts PE Phase 3 smoke for the Settings devices/sessions AJAX actions: boots the real web
/// host, stubs auth + IApiClient, and verifies that AJAX (X-Requested-With: fetch) RevokeSession /
/// TrustDevice return the swappable sessions fragment (root carrying <c>data-yj-swap="sessions"</c>)
/// and RemoveDevice returns the devices fragment (<c>data-yj-swap="devices"</c>), NOT a full-page
/// layout. Also verifies the no-JS path still does a PRG redirect (progressive-enhancement fallback
/// preserved) and that an API failure surfaces as a JSON error (no false success).
/// </summary>
public sealed class AccountsDevicesAjaxSmokeTests
{
    private const string SessionId = "55555555-5555-5555-5555-555555555555";
    private const string DeviceId = "66666666-6666-6666-6666-666666666666";
    private const string TokenId = "77777777-7777-7777-7777-777777777777";

    // One non-current session → the security tab renders a row with Trust/Revoke forms.
    private static readonly string SessionsJson = $$"""
        [
          {
            "sessionId": "{{SessionId}}",
            "deviceId": "{{DeviceId}}",
            "deviceName": "Chrome on Windows",
            "userAgent": "Mozilla/5.0",
            "ipAddress": "203.0.113.5",
            "createdAt": "2024-01-01T00:00:00Z",
            "expiresAt": "2024-02-01T00:00:00Z",
            "isCurrent": false,
            "isTrusted": false
          }
        ]
        """;

    // One device token → the devices tab renders a row with a Remove form.
    private static readonly string DevicesJson = $$"""
        [
          {
            "id": "{{TokenId}}",
            "deviceId": "{{DeviceId}}",
            "platform": "Web",
            "lastSeenAt": "2024-01-01T00:00:00Z",
            "createdAt": "2024-01-01T00:00:00Z"
          }
        ]
        """;

    private static readonly string[] FullPermissions =
    [
        "Permission.DeviceToken.Create",
        "Permission.DeviceToken.Delete",
    ];

    [Fact]
    public async Task AjaxRevokeSession_ReturnsSessionsPartial_NotFullLayout()
    {
        using var factory = new AccountsDevicesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/sessions/revoke/{SessionId}");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"sessions\"",
            "the AJAX revoke must return the swappable sessions fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task AjaxTrustDevice_ReturnsSessionsPartial_NotFullLayout()
    {
        using var factory = new AccountsDevicesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/devices/trust/{DeviceId}");
        request.Headers.Add("X-Requested-With", "fetch");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"sessions\"",
            "trusting a device refreshes the sessions section");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task AjaxRemoveDevice_ReturnsDevicesPartial_NotFullLayout()
    {
        using var factory = new AccountsDevicesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/devices/{TokenId}/remove");
        request.Headers.Add("X-Requested-With", "fetch");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"devices\"",
            "the AJAX remove must return the swappable devices fragment");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task NonAjaxRevokeSession_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsDevicesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/sessions/revoke/{SessionId}");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/settings");
    }

    [Fact]
    public async Task NonAjaxRemoveDevice_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsDevicesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/devices/{TokenId}/remove");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/settings");
    }

    [Fact]
    public async Task AjaxRevokeSession_OnApiFailure_ReturnsError_NoFalseSuccess()
    {
        using var factory = new AccountsDevicesWebFactory(failDeletes: true);
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/settings/sessions/revoke/{SessionId}");
        request.Headers.Add("X-Requested-With", "fetch");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an API failure must surface as a JSON error, not a false success swap");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("error");
        body.Should().NotContain("data-yj-swap=\"sessions\"", "a failed revoke must not swap in a fresh section");
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string url)
    {
        var page = await client.GetAsync(url);
        page.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await page.Content.ReadAsStringAsync();

        const string marker = "name=\"__RequestVerificationToken\"";
        var idx = body.IndexOf(marker, StringComparison.Ordinal);
        idx.Should().BeGreaterThan(-1, "the page must render an anti-forgery field");
        var valueIdx = body.IndexOf("value=\"", idx, StringComparison.Ordinal) + "value=\"".Length;
        var end = body.IndexOf('"', valueIdx);
        return body[valueIdx..end];
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class AccountsDevicesWebFactory : WebApplicationFactory<Program>
    {
        private readonly bool _failDeletes;

        public AccountsDevicesWebFactory(bool failDeletes = false) => _failDeletes = failDeletes;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets();

            var failDeletes = _failDeletes;
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
                services.AddScoped<IApiClient>(_ => new StubApiClient(failDeletes));
            });
        }

        public HttpClient CreateClientFor(string[] permissions)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state => state.Permissions = permissions);
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AccountsDevicesSmokeScheme";
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
                new Claim("sub", "10000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Name, "Smoke Member"),
                new Claim("access_token", accessToken),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        private readonly bool _failDeletes;

        public StubApiClient(bool failDeletes) => _failDeletes = failDeletes;

        // GetSessionsAsync → GetAsync<List<SessionItemResponse>>; GetTokensAsync →
        // GetAsync<List<DeviceTokenResponse>>. Other GETs (notifications/marketing/profile)
        // soft-degrade to empty in their facades, so Fail here is acceptable for page render.
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (typeof(T) == typeof(List<SessionItemResponse>))
                return Deserialize<T>(SessionsJson);
            if (typeof(T) == typeof(List<DeviceTokenResponse>))
                return Deserialize<T>(DevicesJson);

            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        private static Task<ApiResult<T>> Deserialize<T>(string json)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Task.FromResult(data is null
                ? ApiResult<T>.Fail(200, "Could not parse response body.")
                : ApiResult<T>.Ok(data, 200));
        }

        // TrustDevice → PatchAsync. Succeed so the AJAX sessions refresh runs.
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));

        // RevokeSession + RemoveDevice → DeleteAsync(path). Toggle failure for the error-path test.
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(_failDeletes ? ApiResult.Fail(500, "Revoke failed.") : ApiResult.Ok(200));

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
    }
}
