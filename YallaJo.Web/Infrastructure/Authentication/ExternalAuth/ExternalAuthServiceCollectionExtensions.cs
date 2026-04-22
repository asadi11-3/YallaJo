using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
/// </list>
/// </summary>
public static class ExternalAuthServiceCollectionExtensions
{
    public static AuthenticationBuilder AddYallaJoExternalAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthenticationBuilder authBuilder)
    {
        // BFF options + ticket builder.
        services.AddOptions<ExternalAuthOptions>()
            .Bind(configuration.GetSection(ExternalAuthOptions.SectionName));
        services.AddSingleton<IExternalAuthTicketBuilder, ExternalAuthTicketBuilder>();

        // Intermediate cookie scheme used to carry the provider principal from
        // Challenge → callback. MUST be SameSite=None because the provider may
        // redirect via a cross-site POST after consent. Secure=Always protects
        // the cookie in transit.
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
            o.LoginPath = "/auth/login";
        });

        // Google
        var google = new GoogleProviderOptions();
        configuration.GetSection(GoogleProviderOptions.SectionName).Bind(google);
        services.Configure<GoogleProviderOptions>(configuration.GetSection(GoogleProviderOptions.SectionName));
        if (google.IsConfigured)
        {
            authBuilder.AddGoogle(ExternalProviderConstants.Google, o =>
            {
                o.ClientId = google.ClientId;
                o.ClientSecret = google.ClientSecret;
                o.SignInScheme = ExternalProviderConstants.ExternalSignInScheme;
                o.CallbackPath = "/signin-google";
                o.SaveTokens = false;
            });
        }

        // Facebook
        var facebook = new FacebookProviderOptions();
        configuration.GetSection(FacebookProviderOptions.SectionName).Bind(facebook);
        services.Configure<FacebookProviderOptions>(configuration.GetSection(FacebookProviderOptions.SectionName));
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
            });
        }

        return authBuilder;
    }
}

/// <summary>
/// Exposes at runtime which OAuth providers are configured — used by views to
/// render buttons ONLY for providers that actually work.
/// </summary>
public interface IExternalProviderAvailability
{
    bool IsGoogleAvailable { get; }
    bool IsFacebookAvailable { get; }
    IReadOnlyList<string> AvailableProviders { get; }
}

public sealed class ExternalProviderAvailability : IExternalProviderAvailability
{
    public ExternalProviderAvailability(
        GoogleProviderOptions google,
        FacebookProviderOptions facebook)
    {
        IsGoogleAvailable = google.IsConfigured;
        IsFacebookAvailable = facebook.IsConfigured;

        var list = new List<string>(2);
        if (IsGoogleAvailable) list.Add(ExternalProviderConstants.Google);
        if (IsFacebookAvailable) list.Add(ExternalProviderConstants.Facebook);
        AvailableProviders = list;
    }

    public bool IsGoogleAvailable { get; }
    public bool IsFacebookAvailable { get; }
    public IReadOnlyList<string> AvailableProviders { get; }
}
