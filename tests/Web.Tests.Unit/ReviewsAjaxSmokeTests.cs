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
using YallaJo.Web.Areas.Accounts.Models.Reviews;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5 (PE) smoke for the Reviews hub AJAX feedback: boots the real web host, stubs
/// auth + IApiClient, and verifies that AJAX (X-Requested-With: fetch) Edit/Delete return
/// the swappable tab fragment (root carrying <c>data-yj-swap="reviews-mine"</c> /
/// <c>data-yj-swap="reviews-accessibility"</c>), NOT a full-page layout. Also verifies the
/// no-JS path still does a PRG redirect (progressive-enhancement fallback preserved). The
/// eligibility/completed-booking rules stay on the API and are unchanged here.
/// </summary>
public sealed class ReviewsAjaxSmokeTests
{
    private const string ReviewId = "55555555-5555-5555-5555-555555555555";
    private const string AccReviewId = "44444444-4444-4444-4444-444444444444";
    private const string RowVersion = "AAAAAAAAAAA=";

    // One published review owned by the caller → the "My reviews" tab renders rows.
    private static readonly string ReviewPageJson = $$"""
        {
          "items": [
            {
              "id": "{{ReviewId}}",
              "userId": "99999999-9999-9999-9999-999999999999",
              "targetType": "Tour",
              "targetId": "11111111-1111-1111-1111-111111111111",
              "rating": 5,
              "title": "Great tour",
              "content": "Loved it.",
              "visitDate": null,
              "status": "Published",
              "isVerifiedBooking": true,
              "profanityFlagged": false,
              "currentReportCount": 0,
              "lastEditedAt": null,
              "autoHiddenAt": null,
              "createdAt": "2024-01-01T00:00:00Z",
              "helpfulVoteCount": 0,
              "rowVersion": "{{RowVersion}}",
              "replies": []
            }
          ],
          "nextCursor": null
        }
        """;

    // One accessibility review owned by the caller → the accessibility tab renders rows.
    private static readonly string AccReviewPageJson = $$"""
        {
          "items": [
            {
              "id": "{{AccReviewId}}",
              "userId": "99999999-9999-9999-9999-999999999999",
              "targetType": 1,
              "targetId": "22222222-2222-2222-2222-222222222222",
              "rating": 4,
              "title": "Accessible entrance",
              "content": "Ramp available.",
              "visitDate": null,
              "featureTypesCsv": "Wheelchair",
              "status": 0,
              "createdAt": "2024-01-02T00:00:00Z",
              "lastEditedAt": null
            }
          ],
          "nextCursor": null
        }
        """;

    private static readonly string[] FullPermissions =
    [
        "Permission.Review.Read",
        "Permission.Review.Update",
        "Permission.Review.Delete",
        "Permission.AccessibilityReview.Read",
        "Permission.AccessibilityReview.Delete",
    ];

    [Fact]
    public async Task AjaxEdit_ReturnsMyReviewsPartial_NotFullLayout()
    {
        using var factory = new ReviewsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/reviews");

        var request = new HttpRequestMessage(HttpMethod.Post, "/accounts/reviews/edit");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = ReviewId,
            ["Rating"] = "4",
            ["Title"] = "Updated",
            ["Content"] = "Updated content.",
            ["RowVersion"] = RowVersion,
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"reviews-mine\"",
            "the AJAX edit must return the swappable My-reviews fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task AjaxDeleteAccessibilityReview_ReturnsAccessibilityPartial_NotFullLayout()
    {
        using var factory = new ReviewsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/reviews");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/accessibility-reviews/{AccReviewId}/delete");
        request.Headers.Add("X-Requested-With", "fetch");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"reviews-accessibility\"",
            "the AJAX accessibility delete must return the swappable accessibility fragment");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task NonAjaxDelete_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new ReviewsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/accounts/reviews");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/reviews/{ReviewId}/delete");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["rowVersion"] = RowVersion,
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().Contain("/accounts/reviews");
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

    internal sealed class ReviewsWebFactory : WebApplicationFactory<Program>
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
        public const string SchemeName = "ReviewsSmokeScheme";
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
                new Claim("sub", "99999999-9999-9999-9999-999999999999"),
                new Claim(ClaimTypes.Name, "Smoke Reviewer"),
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
            if (typeof(T) == typeof(ReviewPageResponse))
                return Deserialize<T>(ReviewPageJson);
            if (typeof(T) == typeof(MyAccessibilityReviewPageResponse))
                return Deserialize<T>(AccReviewPageJson);

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

        // Edit → PutAsync (non-generic); Delete (normal review) → DeleteAsync(path, body);
        // Delete (accessibility) → DeleteAsync(path). All succeed so the AJAX refresh path is taken.
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));

        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
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
