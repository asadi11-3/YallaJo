using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Registers the external-provider authentication infrastructure on the Web
/// host:
/// <list type="number">
///   <item><description>The BFF options + ticket builder used to talk to the
///   API.</description></item>
///   <item><description>An intermediate cookie scheme that carries the
///   provider-authenticated identity from Challenge to the callback.</description></item>
///   <item><description>Google / Facebook handlers — each registered ONLY when
///   ClientId/Secret are configured so that missing config does not crash the
///   host (the user just does not see the provider buttons).</description></item>
///   <item><description><see cref="IExternalProviderAvailability"/> for views
///   to branch on whether a given provider is actually wired.</description></item>
/// </list>
/// </summary>
public static class ExternalAuthServiceCollectionExtensions
{
    public static AuthenticationBuilder AddYallaJoExternalAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthenticationBuilder authBuilder)
    {
        // ── BFF ticket protocol (shared with the API via ExternalAuth:SigningKey) ─
        services.AddOptions<ExternalAuthOptions>()
            .Bind(configuration.GetSection(ExternalAuthOptions.SectionName));
        services.AddSingleton<IExternalAuthTicketBuilder, ExternalAuthTicketBuilder>();

        // ── Provider options — bound unconditionally so IExternalProviderAvailability
        //    always has a concrete value to inspect (its "IsConfigured" flag decides
        //    whether the UI renders the button). ───────────────────────────────────
        services.Configure<GoogleProviderOptions>(
            configuration.GetSection(GoogleProviderOptions.SectionName));
        services.Configure<FacebookProviderOptions>(
            configuration.GetSection(FacebookProviderOptions.SectionName));
        services.AddSingleton<IExternalProviderAvailability, ExternalProviderAvailability>();

        // ── Intermediate cookie scheme used to carry the provider principal from
        //    Challenge → callback. MUST be SameSite=Lax so that top-level redirects
        //    back from the provider still present the cookie; Secure=Always protects
        //    the cookie in transit. ───────────────────────────────────────────────
        authBuilder.AddCookie(ExternalProviderConstants.ExternalSignInScheme, o =>
        {
            o.Cookie.Name = "YallaJo.Web.ExternalSignIn";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            // The intermediate cookie has no UI — if it ever expires, the user
            // is bounced back to login cleanly.
            o.LoginPath = "/auth/sign-in";
        });

        // ── Google ───────────────────────────────────────────────────────────────
        var google = new GoogleProviderOptions();
        configuration.GetSection(GoogleProviderOptions.SectionName).Bind(google);
        if (google.IsConfigured)
        {
            authBuilder.AddGoogle(ExternalProviderConstants.Google, o =>
            {
                o.ClientId = google.ClientId;
                o.ClientSecret = google.ClientSecret;
                o.SignInScheme = ExternalProviderConstants.ExternalSignInScheme;
                o.CallbackPath = "/signin-google";
                o.SaveTokens = false;

                // Request standard OpenID Connect scopes so the userinfo payload
                // carries the address AND a trustworthy "email_verified" flag.
                o.Scope.Add("openid");
                o.Scope.Add("email");
                o.Scope.Add("profile");

                // Google returns "email_verified" (bool) in the userinfo
                // payload, but ASP.NET Core's default claim mapping does NOT
                // surface it. Auto-linking relies on this claim, so map it
                // explicitly into the external principal.
                o.ClaimActions.MapJsonKey("email_verified", "email_verified", ClaimValueTypes.Boolean);
            });
        }

        // ── Facebook ─────────────────────────────────────────────────────────────
        var facebook = new FacebookProviderOptions();
        configuration.GetSection(FacebookProviderOptions.SectionName).Bind(facebook);
        if (facebook.IsConfigured)
        {
            authBuilder.AddFacebook(ExternalProviderConstants.Facebook, o =>
            {
                o.AppId = facebook.AppId;
                o.AppSecret = facebook.AppSecret;
                o.SignInScheme = ExternalProviderConstants.ExternalSignInScheme;
                o.CallbackPath = "/signin-facebook";
                o.SaveTokens = false;
                o.Fields.Add("email");

                // Meta only releases the email field once the user has
                // confirmed it on their Facebook account (the field is not
                // returned otherwise). When an email IS present we therefore
                // treat it as provider-verified — Facebook has no separate
                // "email_verified" flag to map. The callback handler inspects
                // this synthesized claim exactly like Google's email_verified.
                o.Events.OnCreatingTicket = ctx =>
                {
                    FacebookEmailVerificationSynthesizer.Apply(ctx.Identity);
                    return Task.CompletedTask;
                };
            });
        }

        return authBuilder;
    }
}
