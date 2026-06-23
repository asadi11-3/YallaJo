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
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Accounts PE Phase 4 smoke for the Account Notification Settings: boots the real web host, stubs
/// auth + IApiClient, and verifies that an AJAX (X-Requested-With: fetch) save returns the swappable
/// section fragment (<c>data-yj-swap="notification-settings"</c> / <c>"marketing-settings"</c>), NOT
/// a full-page layout. Also verifies the no-JS path still does a PRG redirect (progressive
/// enhancement fallback preserved), and that an API failure returns 400 JSON (no false success).
/// </summary>
public sealed class AccountsNotificationsAjaxSmokeTests
{
    // One notification preference row + a marketing-consent payload so GET /accounts/settings renders
    // 200 (for token scrape) and the section re-fetch after a save returns populated cards.
    private const string PreferencesJson = """
        [
          { "type": "BookingConfirmed", "channel": "Email", "isEnabled": true },
          { "type": "BookingConfirmed", "channel": "Push", "isEnabled": false },
          { "type": "BookingConfirmed", "channel": "InApp", "isEnabled": true }
        ]
        """;

    private const string MarketingJson = """
        {
          "emailDigest": true,
          "pushNotifications": false,
          "reEngagementCampaigns": true,
          "lastUpdatedUtc": null
        }
        """;

    [Fact]
    public async Task AjaxUpdateNotifications_ReturnsNotificationSectionPartial_NotFullLayout()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/settings/notifications");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["pref_BookingConfirmed_Email"] = "true",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"notification-settings\"",
            "the AJAX update must return the swappable notification-settings fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task AjaxUpdateMarketing_ReturnsMarketingSectionPartial_NotFullLayout()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/settings/marketing");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["emailDigest"] = "true",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"marketing-settings\"",
            "the AJAX update must return the swappable marketing-settings fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task NonAjaxUpdateNotifications_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/settings/notifications");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["pref_BookingConfirmed_Email"] = "true",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        // RedirectToAction(nameof(Index)) yields the conventional area route /Accounts/Settings
        // (capitalized) plus ?tab=notifications; match case-insensitively.
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/settings");
    }

    [Fact]
    public async Task NonAjaxUpdateMarketing_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsNotificationsWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/settings/marketing");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["emailDigest"] = "true",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/settings");
    }

    [Fact]
    public async Task AjaxUpdateNotifications_OnApiFailure_ReturnsError_NoFalseSuccess()
    {
        using var factory = new AccountsNotificationsWebFactory(failUpdates: true);
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/settings");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/settings/notifications");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["pref_BookingConfirmed_Email"] = "true",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an API failure on an AJAX submit must return 400 JSON, not a false-success partial");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("error", "the failure response must carry the JSON error envelope");
        body.Should().NotContain("data-yj-swap=\"notification-settings\"",
            "a failure must not return the success partial");
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
        private readonly bool _failUpdates;

        public AccountsNotificationsWebFactory(bool failUpdates = false) => _failUpdates = failUpdates;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets();

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
                services.AddScoped<IApiClient>(_ => new StubApiClient(_failUpdates));
            });
        }

        public new HttpClient CreateClient()
            => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AccountsNotificationsSmokeScheme";

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // UpdateNotifications / UpdateMarketing only require an authenticated user ([Authorize]).
            var jwt = new JwtSecurityToken();
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            var claims = new[]
            {
                new Claim("sub", "99999999-9999-9999-9999-999999999999"),
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
        private readonly bool _failUpdates;

        public StubApiClient(bool failUpdates) => _failUpdates = failUpdates;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            // GET /api/v1/notifications/preferences (Index render + AJAX section re-fetch).
            if (typeof(T) == typeof(List<NotificationPreferenceResponse>))
                return Deserialize<T>(PreferencesJson);

            // GET /api/v1/accounts/me/marketing-consent.
            if (typeof(T) == typeof(MarketingConsentResponse))
                return Deserialize<T>(MarketingJson);

            // Sessions / devices / profile / promo lookups soft-degrade — the Settings hub tolerates
            // failure (sessions default empty, devices best-effort, profile sidebar empty).
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

        // UpdatePreferencesAsync + UpdateMarketingConsentAsync both post via the non-generic PutAsync.
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(_failUpdates ? ApiResult.Fail(500, "Update failed.") : ApiResult.Ok(200));

        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
    }
}
