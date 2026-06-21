using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
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
/// Accounts PE Phase 2 smoke for the Account Avatar upload/delete: boots the real web host, stubs
/// auth + IApiClient, and verifies that an AJAX (X-Requested-With: fetch) avatar upload/delete
/// returns the swappable <c>_ProfileCard</c> fragment (root carrying
/// <c>data-yj-swap="profile-card"</c>), NOT a full-page layout. Also verifies (a) the no-JS path
/// still does a PRG redirect (progressive enhancement fallback preserved), and (b) an invalid AJAX
/// upload (no file) returns a 400 JSON <c>{ error }</c> with no false success.
/// </summary>
public sealed class AccountsAvatarAjaxSmokeTests
{
    // Valid profile payload so GET /accounts/profile renders 200 (for token scrape) and the
    // refreshed card after a successful avatar change has non-blank name/email.
    private const string ProfileJson = """
        {
          "userId": "99999999-9999-9999-9999-999999999999",
          "firstName": "Smoke",
          "lastName": "Member",
          "displayName": "Smoke Member",
          "avatarUrl": "/media/avatars/smoke.png",
          "phoneNumber": "+97312345678",
          "dateOfBirth": "1990-01-01",
          "gender": null,
          "country": "Bahrain",
          "city": "Manama",
          "addressLine": "1 Test Street",
          "email": "smoke.member@example.com"
        }
        """;

    // Minimal PNG header bytes — content is irrelevant (the API client is stubbed) but the
    // multipart part must declare image/png so the form looks like a real upload.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task AjaxUpload_ReturnsProfileCardPartial_NotFullLayout()
    {
        using var factory = new AccountsAvatarWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/avatar");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = BuildUploadContent(token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"profile-card\"",
            "the AJAX avatar upload must return the swappable profile-card fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task AjaxDelete_ReturnsProfileCardPartial_NotFullLayout()
    {
        using var factory = new AccountsAvatarWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/avatar/delete");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"profile-card\"",
            "the AJAX avatar delete must return the swappable profile-card fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task NonAjaxUpload_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsAvatarWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/avatar");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = BuildUploadContent(token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        // RedirectToAction(nameof(Index)) yields the conventional area route /Accounts/Profile
        // (capitalized); match case-insensitively.
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/profile");
    }

    [Fact]
    public async Task NonAjaxDelete_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new AccountsAvatarWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/avatar/delete");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS delete path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().ContainEquivalentOf("/accounts/profile");
    }

    [Fact]
    public async Task AjaxInvalidUpload_ReturnsError_NoFalseSuccess()
    {
        using var factory = new AccountsAvatarWebFactory();
        var client = factory.CreateClient();

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/profile");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/profile/avatar");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        // No file part → [Required] File fails ModelState → BadRequest JSON { error }.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an invalid AJAX avatar upload must return 400 with a JSON error, never a false success");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("error", "the failure response must carry an error field for the toast");
        body.Should().NotContain("data-yj-swap=\"profile-card\"",
            "an invalid upload must not return a success partial that would be swapped in");
    }

    private static MultipartFormDataContent BuildUploadContent(string token)
    {
        var content = new MultipartFormDataContent();

        var file = new ByteArrayContent(PngBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "File", "avatar.png");

        content.Add(new StringContent(token), "__RequestVerificationToken");
        return content;
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

    internal sealed class AccountsAvatarWebFactory : WebApplicationFactory<Program>
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
        public const string SchemeName = "AccountsAvatarSmokeScheme";

        public TestAuthHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Profile/avatar only requires an authenticated user ([Authorize], no specific permission).
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

        // UpdateAvatarAsync → PutFileAsync<object> (HTTP PUT multipart) → drive a successful upload.
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
        {
            // T is object here (ProfileApiClient.UpdateAvatarAsync uses PutFileAsync<object>); an
            // empty JSON object deserializes to a non-null value, so the success result is valid.
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                "{}", new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return Task.FromResult(ApiResult<T>.Ok(data, 200));
        }

        // DeleteAvatarAsync → DeleteAsync (and DeleteProfileAsync) → succeed.
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));

        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
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
        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
    }
}
