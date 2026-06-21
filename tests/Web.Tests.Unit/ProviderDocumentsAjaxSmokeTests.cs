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
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 3 (PE) smoke for provider document AJAX small actions: boots the real web host,
/// stubs auth + IApiClient, and verifies that AJAX (X-Requested-With: fetch) upload returns
/// the swappable <c>_DocumentsManager</c> fragment (root carrying <c>data-yj-swap="documents"</c>),
/// NOT a full-page layout. Also verifies the no-JS path still does a PRG redirect
/// (progressive-enhancement fallback preserved). The download path stays server-mediated and
/// is intentionally NOT AJAX (StorageKey/private paths must never be exposed).
/// </summary>
public sealed class ProviderDocumentsAjaxSmokeTests
{
    private const string DocumentId = "66666666-6666-6666-6666-666666666666";

    // Draft status with one document → CanUploadDocuments=true so upload + replace forms render.
    private static readonly string StatusJson = $$"""
        {
          "applicationId": "11111111-1111-1111-1111-111111111111",
          "type": "TourOperator",
          "businessName": "Acme Tours",
          "status": "Draft",
          "submittedAt": null,
          "reviewedAt": null,
          "rejectionReason": null,
          "suspensionReason": null,
          "reapplicationCount": 0,
          "coolingPeriodEndsAt": null,
          "documents": [
            { "documentId": "{{DocumentId}}", "documentType": "BusinessLicense", "fileName": "license.pdf", "expiresAt": null }
          ]
        }
        """;

    private static readonly string[] FullPermissions =
    [
        "Permission.ProviderApplication.Read",
        "Permission.ProviderApplication.Create",
        "Permission.ProviderApplication.Update",
    ];

    [Fact]
    public async Task AjaxUpload_ReturnsDocumentsManagerPartial_NotFullLayout()
    {
        using var factory = new DocumentsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/provider/documents");

        var request = new HttpRequestMessage(HttpMethod.Post, "/provider/documents/upload");
        request.Headers.Add("X-Requested-With", "fetch"); // triggers WantsAjax()
        request.Content = BuildUploadMultipart(token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("data-yj-swap=\"documents\"",
            "the AJAX upload must return the swappable documents fragment for provider-actions.js");
        html.Should().NotContain("<html", "the AJAX branch must return a partial, not a full-page layout");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task NonAjaxUpload_StillRedirects_PrgFallbackPreserved()
    {
        using var factory = new DocumentsWebFactory();
        var client = factory.CreateClientFor(FullPermissions);

        var token = await GetAntiForgeryTokenAsync(client, "/provider/documents");

        var request = new HttpRequestMessage(HttpMethod.Post, "/provider/documents/upload");
        // No X-Requested-With header → server treats as a normal browser POST.
        request.Content = BuildUploadMultipart(token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "the no-JS path must keep the PRG redirect fallback");
        response.Headers.Location!.ToString().Should().Contain("/provider/documents");
    }

    private static MultipartFormDataContent BuildUploadMultipart(string token)
    {
        var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "new-license.pdf");
        content.Add(new StringContent("BusinessLicense"), "DocumentType");
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

    internal sealed class DocumentsWebFactory : WebApplicationFactory<Program>
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
        public const string SchemeName = "ProviderDocumentsSmokeScheme";
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
            // Provider status: GET /api/v1/provider/status
            if (typeof(T) == typeof(ProviderStatusResponse))
                return Deserialize<T>(StatusJson);

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

        // Upload + replace go through PostFileAsync<T>; succeed so the AJAX refresh path is taken.
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
        {
            if (typeof(T) == typeof(AddProviderDocumentResponse))
                return Deserialize<T>($"{{ \"documentId\": \"{DocumentId}\", \"documentType\": \"BusinessLicense\" }}");
            if (typeof(T) == typeof(ReplaceProviderDocumentResponse))
                return Deserialize<T>($"{{ \"documentId\": \"{DocumentId}\" }}");
            return Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        }

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
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
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
    }
}
