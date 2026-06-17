using System.Net;
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
/// User Navigation / Layout coverage smoke for the shared public navbar
/// (<c>~/Views/Shared/_Navbar.cshtml</c>, rendered by <c>_Layout.cshtml</c>).
///
/// Boots the real YallaJo.Web host with <see cref="WebApplicationFactory{T}"/> as an
/// ANONYMOUS visitor (no auth) and replaces <see cref="IApiClient"/> with a stub that
/// returns the "absent" 200 semantics for every read, so the public facades degrade
/// gracefully (empty lists) and the pages still render their chrome.
///
/// Verifies that the public navbar now links to the full confirmed set of existing
/// anonymous Public-area GET pages (Home, Tours, Places, Businesses, Packages, Blog,
/// Guides, Agencies, Contact), that the Blog link resolves to the real <c>/blog</c>
/// route, that tag helpers are processed (no raw <c>asp-*</c> emitted), and that
/// <c>/blog</c> itself does not 404/500.
/// </summary>
public sealed class PublicNavbarSmokeTests
{
    [Fact]
    public async Task AnonymousHome_Returns200_AndRendersPublicNavbarChrome()
    {
        using var factory = new NavbarWebFactory();
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the public homepage must render for anonymous visitors");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotContain("asp-action", "tag helpers must be processed, not emitted raw");
        html.Should().NotContain("asp-controller", "tag helpers must be processed, not emitted raw");
        html.Should().NotContain("<permission", "the permission tag helper must be processed, not emitted raw");
    }

    [Fact]
    public async Task PublicNavbar_ContainsBlogLink_ToBlogRoute()
    {
        using var factory = new NavbarWebFactory();
        var client = factory.CreateAnonymousClient();

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        // The reported gap: Blog must be discoverable from the public navbar and point at /blog.
        html.Should().MatchRegex("href=\"/blog\"", "the navbar must contain a Blog link to the canonical /blog route");
    }

    // Home (Public/Home/Index) is multi-routed ("", "explore", "home"); the tag helper
    // generates the "/explore" alias for it — still the same anonymous home page.
    [Theory]
    [InlineData("/explore")]    // Home (alias of "/")
    [InlineData("/tours")]      // Tours
    [InlineData("/places")]     // Places
    [InlineData("/businesses")] // Businesses / Directory
    [InlineData("/packages")]   // Packages
    [InlineData("/blog")]       // Blog
    [InlineData("/guides")]     // Guides
    [InlineData("/agency")]     // Agencies
    [InlineData("/contact")]    // Contact
    public async Task PublicNavbar_ContainsLink_ForEachConfirmedPublicPage(string route)
    {
        using var factory = new NavbarWebFactory();
        var client = factory.CreateAnonymousClient();

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        html.Should().Contain($"href=\"{route}\"",
            $"the public navbar must link to the existing public page {route}");
    }

    [Fact]
    public async Task PublicNavbar_ContainsCorePublicLinks()
    {
        using var factory = new NavbarWebFactory();
        var client = factory.CreateAnonymousClient();

        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        foreach (var route in new[] { "/tours", "/places", "/packages", "/contact", "/businesses" })
            html.Should().Contain($"href=\"{route}\"", $"core public link {route} must be present");
    }

    [Fact]
    public async Task BlogList_DoesNotReturnNotFoundOrServerError()
    {
        using var factory = new NavbarWebFactory();
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/blog");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the public blog list must render (no 404/500)");
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    // ── Test host ──────────────────────────────────────────────────────────────

    private sealed class NavbarWebFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Api:BaseUrl", "http://127.0.0.1:1");
            builder.UseFakeTestSecrets(); // Patch 0A.2: boot host without ambient user-secrets

            builder.ConfigureServices(services =>
            {
                // Register a permissive test auth scheme so the pipeline has a valid scheme,
                // but every request below is issued anonymously (no auth cookie/header).
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                });

                services.RemoveAll<IApiClient>();
                services.AddScoped<IApiClient, GracefulStubApiClient>();
            });
        }

        public HttpClient CreateAnonymousClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<TestAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<TestAuthSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "NavbarSmokeTestScheme";

        // Always anonymous: the navbar's public links are rendered for everyone, and the
        // homepage/blog list are [AllowAnonymous]. Failing authentication keeps the
        // ICurrentUser unauthenticated without challenging public GETs.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }

    /// <summary>
    /// In-memory <see cref="IApiClient"/> stub that returns the real ApiClient "absent" 200
    /// semantics for every read, so the public facades render with empty data (no live API).
    /// </summary>
    private sealed class GracefulStubApiClient : IApiClient
    {
        public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));

        public Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult<ApiFile>.Fail(200, "Empty response body."));
        public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
        public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
        public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
        public Task<ApiResult<T>> PutFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        public Task<ApiResult<T>> PostFileAsync<T>(string path, Stream fileStream, string fileName, string contentType, IReadOnlyDictionary<string, string>? formFields = null, string formFieldName = "file", CancellationToken ct = default)
            => Task.FromResult(ApiResult<T>.Fail(200, "Empty response body."));
        public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
        public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default)
            => Task.FromResult(ApiResult.Ok());
    }
}
