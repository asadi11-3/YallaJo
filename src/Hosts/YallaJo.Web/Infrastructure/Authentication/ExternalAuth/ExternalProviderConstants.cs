namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Canonical provider names shared between the Web BFF and the API
/// <c>ExternalAuth:AllowedProviders</c>. Add a new constant and a matching
/// registration in <c>ExternalAuthServiceCollectionExtensions</c> to extend.
/// </summary>
public static class ExternalProviderConstants
{
    public const string Google = "google";
    public const string Facebook = "facebook";

    /// <summary>
    /// ASP.NET Core external-authentication scheme used when signing in the
    /// user against an OAuth provider. One temporary intermediate cookie
    /// carries the external identity into the callback handler; the callback
    /// then issues a signed ticket and clears the intermediate cookie.
    /// </summary>
    public const string ExternalSignInScheme = "ExternalSignInScheme";
}
