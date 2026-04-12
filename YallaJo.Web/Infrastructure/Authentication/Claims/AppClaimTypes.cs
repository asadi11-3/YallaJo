namespace YallaJo.Web.Infrastructure.Authentication.Claims;

/// <summary>
/// Claim type constants stored in the web app's authentication cookie.
/// Tokens are encrypted server-side — never exposed to the browser.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>Authenticated user's unique identifier (maps to JWT "sub").</summary>
    public const string UserId = "sub";

    /// <summary>Short-lived JWT access token used as Bearer on backend API calls.</summary>
    public const string AccessToken = "access_token";

    /// <summary>Long-lived opaque refresh token used to obtain a new access token.</summary>
    public const string RefreshToken = "refresh_token";

    /// <summary>UTC expiry of the refresh token (ISO 8601 round-trip format "O").</summary>
    public const string RefreshTokenExpiresAt = "refresh_token_expires_at";
}
