using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Accounts.Contracts.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Accounts.IntegrationTests;

/// <summary>
/// Patch 2F: the legacy URL/JSON provider-document endpoints
/// (POST /api/v1/provider/documents and PUT /api/v1/provider/documents/{id})
/// were hard-removed. They accepted a caller-supplied FileUrl with no magic-byte
/// validation and no FileAsset materialization. These tests prove those routes are
/// now unmapped and return 404 even for an authenticated, permitted caller
/// (a 404 here means "route not found", NOT 401 "unauthenticated").
/// The supported multipart routes (/documents/upload, /documents/{id}/replace-upload)
/// remain mapped and are covered by other suites.
/// </summary>
public sealed class ProviderDocumentLegacyJsonEndpointsRemovedTests
{
    [Fact]
    public async Task Legacy_json_add_document_endpoint_returns_404()
    {
        await using var factory = new LegacyRemovedFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/provider/documents", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Legacy_json_replace_document_endpoint_returns_404()
    {
        await using var factory = new LegacyRemovedFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"/api/v1/provider/documents/{Guid.NewGuid()}", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class LegacyRemovedFactory : WebApplicationFactory<Program>
    {
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";

        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(LegacyRemovedAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, LegacyRemovedAuthHandler>(
                        LegacyRemovedAuthHandler.SchemeName, _ => { });

                services.PostConfigureAll<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = LegacyRemovedAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = LegacyRemovedAuthHandler.SchemeName;
                    options.DefaultScheme = LegacyRemovedAuthHandler.SchemeName;
                    options.DefaultForbidScheme = LegacyRemovedAuthHandler.SchemeName;
                });
            });
        }
    }

    private sealed class LegacyRemovedAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AccountsProviderLegacyRemovedTestScheme";

        private static readonly Guid CallerUserId = Guid.Parse("0a000000-0000-0000-0000-0000000000f2");

        public LegacyRemovedAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Authenticated + permitted: proves the 404 is route-not-found, not 401.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, CallerUserId.ToString()),
                new("sub", CallerUserId.ToString()),
                new("Permission", PermissionPolicyNames.Build(AccountsFeatures.ProviderApplication, AppAction.Create)),
                new("Permission", PermissionPolicyNames.Build(AccountsFeatures.ProviderApplication, AppAction.Update)),
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
