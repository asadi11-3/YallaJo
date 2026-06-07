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
/// Authenticated runtime smoke for the provider invoices page (FE-2B-3):
/// with Invoice.Read the page loads (200) and shows the empty state; the sidebar
/// "Invoices" link is gated on Invoice.Read.
/// </summary>
public sealed class ProviderInvoicesSmokeTests
{
    [Fact]
    public async Task WithInvoiceRead_PageLoads_ShowsEmptyState_AndSidebarLink()
    {
        using var factory = new ProviderInvoicesWebFactory();
        var client = factory.CreateClientFor(["Permission.Invoice.Read"], """{"items":[],"nextCursor":null}""");

        var response = await client.GetAsync("/provider/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.Should().Contain("No invoices yet", "the empty state must render when there are no invoices");
        html.Should().Contain("/provider/invoices", "the gated sidebar Invoices link must render for Invoice.Read holders");
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
    }

    [Fact]
    public async Task WithoutInvoiceRead_IsForbidden()
    {
        using var factory = new ProviderInvoicesWebFactory();
        var client = factory.CreateClientFor(["Permission.SomethingElse.Read"], """{"items":[],"nextCursor":null}""");

        var response = await client.GetAsync("/provider/invoices");

        // The controller is [RequirePermission(Invoice.Read)] → 403 without it.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Test host ─────────────────────────────────────────────────────────────

    internal sealed class ProviderInvoicesWebFactory : WebApplicationFactory<Program>
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

        public HttpClient CreateClientFor(string[] permissions, string? invoicesJson)
        {
            var factory = WithWebHostBuilder(b =>
                b.ConfigureServices(s =>
                {
                    s.Configure<TestAuthState>(state => state.Permissions = permissions);
                    s.Configure<TestApiState>(state => state.InvoicesJson = invoicesJson);
                }));

            return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
    }

    private sealed class TestAuthState { public string[] Permissions { get; set; } = []; }
    private sealed class TestApiState { public string? InvoicesJson { get; set; } }
    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions { }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "ProviderInvoicesSmokeScheme";
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
        private readonly TestApiState _state;
        public StubApiClient(IOptions<TestApiState> state) => _state = state.Value;

        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
        {
            if (path.StartsWith("/api/v1/invoices/provider/my-invoices", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(_state.InvoicesJson))
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<T>(
                    _state.InvoicesJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Task.FromResult(data is null
                    ? ApiResult<T>.Fail(200, "Could not parse response body.")
                    : ApiResult<T>.Ok(data, 200));
            }

            return Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        }

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(501, "not implemented in stub"));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(501, "not implemented in stub"));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Fail(501, "not implemented in stub"));
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
