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
using YallaJo.Web.Areas.Provider.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 2 (PE) smoke for provider package AJAX small actions: boots the real web host,
/// stubs auth + IApiClient, and verifies that AJAX (X-Requested-With: fetch) delete returns
/// the swappable <c>_PackagesList</c> fragment (root carrying <c>data-yj-swap="packages"</c>)
/// and AJAX add-inclusion returns the <c>_Inclusions</c> fragment
/// (root carrying <c>data-yj-swap="inclusions"</c>), NOT full-page layouts. Also verifies the
/// no-JS path still does a PRG redirect (progressive-enhancement fallback preserved).
/// </summary>
public sealed class ProviderPackagesAjaxSmokeTests
{
    private const string PackageId = "44444444-4444-4444-4444-444444444444";

    private static readonly string PackageListJson = $$"""
        {
          "items": [
            {
              "id": "{{PackageId}}",
              "name": "Desert Adventure",
              "description": "A great package",
              "priceAmount": 120.000,
              "currency": "JOD",
              "maxParticipants": 10,
              "validFrom": null,
              "validTo": null,
              "includedTourCount": 2,
              "createdAt": "2026-01-01T00:00:00Z"
            }
          ],
          "pageNumber": 1,
          "pageSize": 20,
          "totalCount": 1,
          "hasPreviousPage": false,
          "hasNextPage": false
        }
        """;

    private static readonly string PackageDetailJson = $$"""
        {
          "id": "{{PackageId}}",
          "name": "Desert Adventure",
          "description": "A great package",
          "priceAmount": 120.000,
          "currency": "JOD",
          "maxParticipants": 10,
          "validFrom": null,
          "validTo": null,
          "isActive": false,
          "createdAt": "2026-01-01T00:00:00Z",
          "includedTours": [],
          "inclusions": [
            { "id": "55555555-5555-5555-5555-555555555555", "description": "Breakfast included", "sortOrder": 0 }
          ]
        }
        """;

    private static readonly string[] FullPermissions =
    [
        "Permission.Package.Read",
        "Permission.Package.Create",
        "Permission.Package.Update",
        "Permission.Package.Delete",
    ];

    [Fact]
    public async Task AjaxDelete_ReturnsPackagesListPartial_NotFullLayout()
    {
        using var factory = new PackagesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/provider/packages");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/provider/packages/{PackageId}/delete");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"packages\"",
            "the AJAX delete must return the swappable packages list fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task AjaxAddInclusion_ReturnsInclusionsPartial_NotFullLayout()
    {
        using var factory = new PackagesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, $"/provider/packages/{PackageId}");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/provider/packages/{PackageId}/inclusions");
        request.Headers.Add("X-Requested-With", "fetch");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["description"] = "Lunch included",
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"inclusions\"",
            "the AJAX add-inclusion must return the swappable inclusions fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
    }

    [Fact]
    public async Task NonAjaxDelete_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new PackagesWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/provider/packages");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/provider/packages/{PackageId}/delete");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().Contain("/provider/packages");
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

    internal sealed class PackagesWebFactory : WebApplicationFactory<Program>
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
        public const string SchemeName = "ProviderPackagesSmokeScheme";
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
                new Claim(ClaimTypes.Name, "Smoke Provider"),
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
            // Package detail: GET /api/v1/tours/packages/{id}
            if (typeof(T) == typeof(PackageDetailResponse))
                return Deserialize<T>(PackageDetailJson);

            // Package list: GET /api/v1/tours/packages?page=...
            if (typeof(T) == typeof(PackageListResponse))
                return Deserialize<T>(PackageListJson);

            // Tour options lookup soft-degrades to an empty picker when it fails (facade ERR3).
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

        // Delete + add-inclusion succeed so the controller takes the AJAX refresh path.
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok(200));

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
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
    }
}
