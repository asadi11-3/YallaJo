using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

namespace Web.Tests.Unit;

/// <summary>
/// Verifies the external-provider wiring on the Web host:
///
/// <list type="bullet">
///   <item><description>Registers BOTH Google and Facebook handlers when both
///   are configured. A previous regression caused the Facebook button to do
///   nothing because its handler wasn't being resolved.</description></item>
///   <item><description>Uses the middleware-default <c>CallbackPath</c>s
///   (<c>/signin-google</c>, <c>/signin-facebook</c>) as the redirect URIs —
///   these are what must be whitelisted on the provider side.</description></item>
///   <item><description>Requests the OpenID Connect email scopes for Google
///   so the userinfo payload carries <c>email_verified</c>.</description></item>
///   <item><description>Facebook's <see cref="FacebookEmailVerificationSynthesizer"/>
///   synthesizes <c>email_verified=true</c> when Meta returned an email, so
///   auto-link can treat it uniformly with Google's attestation.</description></item>
/// </list>
/// </summary>
public sealed class ExternalAuthProviderWiringTests
{
    private static IServiceProvider BuildServices(
        string? googleClientId = "google-client-id",
        string? googleClientSecret = "google-client-secret",
        string? facebookAppId = "fb-app-id",
        string? facebookAppSecret = "fb-app-secret")
    {
        var dict = new Dictionary<string, string?>
        {
            ["ExternalAuth:SigningKey"] = "YallaJo-ExternalAuth-Local-Dev-Key-2026-Strong-Secret",
            ["ExternalAuth:Issuer"] = "YallaJo.Web",
            ["ExternalAuth:Audience"] = "YallaJo.Api",
            ["ExternalAuth:TicketLifetimeSeconds"] = "120",
            ["ExternalAuth:AllowedProviders:0"] = "google",
            ["ExternalAuth:AllowedProviders:1"] = "facebook",
            ["ExternalProviders:Google:ClientId"] = googleClientId ?? string.Empty,
            ["ExternalProviders:Google:ClientSecret"] = googleClientSecret ?? string.Empty,
            ["ExternalProviders:Facebook:AppId"] = facebookAppId ?? string.Empty,
            ["ExternalProviders:Facebook:AppSecret"] = facebookAppSecret ?? string.Empty,
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var authBuilder = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();

        services.AddYallaJoExternalAuth(configuration, authBuilder);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddYallaJoExternalAuth_ShouldRegister_GoogleAndFacebook_WhenBothConfigured()
    {
        using var sp = (ServiceProvider)BuildServices();
        var schemes = sp.GetRequiredService<IAuthenticationSchemeProvider>();

        var all = schemes.GetAllSchemesAsync().GetAwaiter().GetResult();
        var names = all.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

        names.Should().Contain(ExternalProviderConstants.Google,
            "the Google button is non-functional without a registered Google scheme");
        names.Should().Contain(ExternalProviderConstants.Facebook,
            "the Facebook button is non-functional without a registered Facebook scheme — " +
            "this was the reported 'Facebook does nothing' regression");
        names.Should().Contain(ExternalProviderConstants.ExternalSignInScheme);
    }

    [Fact]
    public void AddYallaJoExternalAuth_ShouldSkipProvider_WhenNotConfigured()
    {
        using var sp = (ServiceProvider)BuildServices(
            facebookAppId: string.Empty, facebookAppSecret: string.Empty);
        var schemes = sp.GetRequiredService<IAuthenticationSchemeProvider>();

        var all = schemes.GetAllSchemesAsync().GetAwaiter().GetResult();
        var names = all.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

        names.Should().Contain(ExternalProviderConstants.Google);
        names.Should().NotContain(ExternalProviderConstants.Facebook,
            "missing Facebook config must not register the handler (so the " +
            "button simply does not render in the UI)");
    }

    [Fact]
    public void Google_ShouldUse_StandardMiddlewareCallbackPath()
    {
        using var sp = (ServiceProvider)BuildServices();
        var monitor = sp.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
        var opts = monitor.Get(ExternalProviderConstants.Google);

        opts.CallbackPath.Value.Should().Be("/signin-google",
            "the OAuth redirect whitelisted in Google Cloud Console targets the " +
            "middleware CallbackPath, not the in-app /auth/external/callback/* route");
        opts.SignInScheme.Should().Be(ExternalProviderConstants.ExternalSignInScheme);
        opts.SaveTokens.Should().BeFalse("provider access tokens aren't needed once the BFF ticket is minted");
        opts.Scope.Should().Contain("openid");
        opts.Scope.Should().Contain("email");
        opts.Scope.Should().Contain("profile");
    }

    [Fact]
    public void Facebook_ShouldUse_StandardMiddlewareCallbackPath_AndRequestEmail()
    {
        using var sp = (ServiceProvider)BuildServices();
        var monitor = sp.GetRequiredService<IOptionsMonitor<FacebookOptions>>();
        var opts = monitor.Get(ExternalProviderConstants.Facebook);

        opts.CallbackPath.Value.Should().Be("/signin-facebook");
        opts.SignInScheme.Should().Be(ExternalProviderConstants.ExternalSignInScheme);
        opts.Fields.Should().Contain("email",
            "auto-link depends on the provider-asserted email address");
    }

    // ─────────────────────────────────────────────────────────────────────
    // FacebookEmailVerificationSynthesizer — unit tests. The Facebook
    // OnCreatingTicket event delegates to this helper, so covering the
    // helper directly is equivalent and doesn't need a real OAuth context.
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Synthesizer_ShouldAdd_EmailVerifiedTrue_WhenEmailPresent()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "fb-user-1"),
            new Claim(ClaimTypes.Email, "user@facebook.com"),
        }, "Facebook");

        FacebookEmailVerificationSynthesizer.Apply(identity);

        var claim = identity.FindFirst("email_verified");
        claim.Should().NotBeNull(
            "Meta only returns the email field once the user has confirmed it, " +
            "so presence of email IS the verification signal");
        claim!.Value.Should().Be("true");
    }

    [Fact]
    public void Synthesizer_ShouldNotAdd_WhenEmailMissing()
    {
        // If Meta withheld the email (user scoped the permission away), do
        // NOT fabricate a verified flag. Auto-link will then correctly refuse.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "fb-user-1"),
        }, "Facebook");

        FacebookEmailVerificationSynthesizer.Apply(identity);

        identity.FindFirst("email_verified").Should().BeNull();
    }

    [Fact]
    public void Synthesizer_ShouldNotOverwrite_ExistingEmailVerifiedClaim()
    {
        // Defense in depth: if some other hook already set the claim,
        // don't clobber it.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "user@facebook.com"),
            new Claim("email_verified", "false", ClaimValueTypes.Boolean),
        }, "Facebook");

        FacebookEmailVerificationSynthesizer.Apply(identity);

        identity.FindAll("email_verified").Should().ContainSingle()
            .Which.Value.Should().Be("false");
    }

    [Fact]
    public void Synthesizer_ShouldNoop_OnNullIdentity()
    {
        FacebookEmailVerificationSynthesizer.Apply(null);
        // Must not throw.
        true.Should().BeTrue();
    }
}
