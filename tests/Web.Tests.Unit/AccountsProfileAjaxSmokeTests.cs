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
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Accounts PE Phase 1 smoke for the Account Profile save: boots the real web host, stubs auth +
/// IApiClient, and verifies that an AJAX (X-Requested-With: fetch) profile update returns the
/// swappable <c>_ProfileCard</c> fragment (root carrying <c>data-yj-swap="profile-card"</c>), NOT a
/// full-page layout. Also verifies (a) the no-JS path still does a PRG redirect (progressive
/// enhancement fallback preserved), and (b) an invalid AJAX submit returns the <c>_ProfileCard</c>
/// fragment with a 400 status and inline validation errors (no success toast on the client).
/// </summary>
public sealed class AccountsProfileAjaxSmokeTests
{
    // Valid profile payload so GET /accounts/profile renders 200 (for token scrape) and the
    // refreshed card has non-blank name/email after a successful save.
    private const string ProfileJson = """
        {
          "userId": "99999999-9999-9999-9999-999999999999",
          "firstName": "Smoke",
          "lastName": "Member",
          "displayName": "Smoke Member",
          "avatarUrl": null,
          "phoneNumber": "+97312345678",
          "dateOfBirth": "1990-01-01",
          "gender": null,
          "country": "Bahrain",
          "city": "Manama",
          "addressLine": "1 Test Street",
          "email": "smoke.member@example.com"
        }
        """;

    [Fact]
    public async Task AjaxValidUpdate_ReturnsProfileCardPartial_NotFullLayout()
    {
        using var factory = new AccountsProfileWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/update");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Valid",
            ["LastName"] = "User",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"profile-card\"",
            "the AJAX update must return the swappable profile-card fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task NonAjaxUpdate_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsProfileWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/update");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Valid",
            ["LastName"] = "User",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        // RedirectToAction(nameof(Index)) yields the conventional area route /Accounts/Profile
        // (capitalized); match case-insensitively.
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/profile");
    }

    [Fact]
    public async Task AjaxInvalidUpdate_ReturnsProfileCardPartial_With400()
    {
        using var factory = new AccountsProfileWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/update");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            // FirstName intentionally blank → [Required] validation error.
            ["LastName"] = "User",
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an invalid AJAX submit must return 400 with the re-rendered card carrying inline errors");
        (response.Content.Headers.ContentType?.MediaType ?? string.Empty)
            .Should().Contain("text/html", "the validation response must be the HTML fragment, not JSON");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("data-yj-swap=\"profile-card\"",
            "the 400 response must still be the swappable profile-card fragment so provider-actions.js can swap it");
        html.Should().Contain("text-danger",
            "the re-rendered card must surface the inline validation error styling");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
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

    internal sealed class AccountsProfileWebFactory : WebApplicationFactory<Program>
    {
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
                services.AddScoped<IApiClient, StubApiClient>();
            });
        }

        public new HttpClient CreateClient()
            => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "AccountsProfileSmokeScheme";

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Profile only requires an authenticated user ([Authorize], no specific permission).
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
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            // Profile: GET /api/v1/accounts/profile (Index render + the AJAX re-fetch both call this).
            if (typeof(T) == typeof(ProfileResponse))
                return Deserialize<T>(ProfileJson);

            // Promo blocks and any other lookup soft-degrade — the controller tolerates failure
            // (LoadPromosAsync falls back to an empty list).
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

        // UpdateProfileAsync posts via the non-generic PutAsync → drive a successful save.
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));

        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
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
