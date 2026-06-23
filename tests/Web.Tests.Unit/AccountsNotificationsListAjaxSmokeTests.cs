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
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Accounts PE Phase 5 smoke for the notifications list Delete AJAX action: boots the real web host,
/// stubs auth + IApiClient, and verifies that an AJAX (X-Requested-With: fetch) Delete returns the
/// swappable list fragment (root carrying <c>data-yj-swap="notifications-list"</c>), NOT a full-page
/// layout. Also verifies the no-JS path still does a PRG redirect (progressive-enhancement fallback
/// preserved) and that an API failure surfaces as a JSON error (no false success swap).
/// </summary>
public sealed class AccountsNotificationsListAjaxSmokeTests
{
    private const string NotificationId = "55555555-5555-5555-5555-555555555555";

    // One unread notification → the list renders a row with the Delete form.
    private static readonly string InboxJson = $$"""
        {
          "items": [
            {
              "id": "{{NotificationId}}",
              "type": "BookingConfirmed",
              "title": "Booking confirmed",
              "body": "Your booking is confirmed.",
              "isRead": false,
              "entityType": null,
              "entityId": null,
              "createdAt": "2024-01-01T00:00:00Z"
            }
          ],
          "nextCursor": null,
          "totalCount": 1
        }
        """;

    private static readonly string[] FullPermissions =
    [
        "Permission.Notification.Read",
        "Permission.Notification.Delete",
    ];

    [Fact]
    public async Task AjaxDelete_ReturnsNotificationsListPartial_NotFullLayout()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/notifications");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/notifications/{NotificationId}/delete");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"notifications-list\"",
            "the AJAX delete must return the swappable list fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task NonAjaxDelete_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/notifications");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/notifications/{NotificationId}/delete");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/notifications");
    }

    [Fact]
    public async Task AjaxDelete_OnApiFailure_ReturnsError_NoFalseSuccess()
    {
        using var factory = new AccountsNotificationsWebFactory(failDeletes: true);
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/notifications");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/notifications/{NotificationId}/delete");
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
        body.Should().NotContain("data-yj-swap=\"notifications-list\"", "a failed delete must not swap in a fresh list");
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

    internal sealed class AccountsNotificationsWebFactory : WebApplicationFactory<Program>
    {
        private readonly bool _failDeletes;

        public AccountsNotificationsWebFactory(bool failDeletes = false) => _failDeletes = failDeletes;

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
        public const string SchemeName = "AccountsNotificationsSmokeScheme";
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

        // GetListAsync → GetAsync<NotificationPageResponse>; GetUnreadCountAsync → GetAsync<int>.
        // Other GETs (profile sidebar) soft-degrade, so Fail here is acceptable for page render.
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (typeof(T) == typeof(NotificationPageResponse))
                return Deserialize<T>(InboxJson);
            if (typeof(T) == typeof(int))
                return Task.FromResult(ApiResult<T>.Ok((T)(object)1, 200));

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

        // Delete → DeleteAsync(path). Toggle failure for the error-path test.
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(_failDeletes ? ApiResult.Fail(500, "Delete failed.") : ApiResult.Ok(200));

        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
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
