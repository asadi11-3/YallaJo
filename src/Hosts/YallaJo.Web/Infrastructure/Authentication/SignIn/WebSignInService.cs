using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.Claims;

namespace YallaJo.Web.Infrastructure.Authentication.SignIn;

public sealed class WebSignInService : IWebSignInService
{
    private readonly IHttpContextAccessor _accessor;

    public WebSignInService(IHttpContextAccessor accessor) =>
        _accessor = accessor;

    public async Task SignInAsync(
        Guid userId,
        string accessToken,
        string refreshToken,
        DateTime refreshTokenExpiresAt)
    {
        var context = _accessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP context.");

        // Core token-storage claims only.
        //
        // We deliberately do NOT copy the JWT's role/Permission claims into the
        // cookie. Admin roles carry ~300+ permission claims; duplicating them here
        // (on top of the embedded access_token, which already contains them) inflated
        // the encrypted cookie to ~50 KB / 13 chunks and tripped Kestrel's 32 KB
        // request-header limit (HTTP 431) on every authenticated request.
        //
        // Permissions/roles are now derived on demand from the access_token JWT by
        // CurrentUser (cached per request). The access_token remains the single
        // source of truth and is never exposed to the browser (HttpOnly cookie).
        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId,                userId.ToString()),
            new(AppClaimTypes.AccessToken,           accessToken),
            new(AppClaimTypes.RefreshToken,          refreshToken),
            new(AppClaimTypes.RefreshTokenExpiresAt, refreshTokenExpiresAt.ToString("O")),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8),
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        // Make the new principal available to the rest of this request immediately.
        context.User = principal;
    }

    public async Task SignOutAsync()
    {
        var context = _accessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP context.");

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the JWT (without signature validation — we issued it ourselves) and returns
    /// all "role" and "Permission" claims. These are NOT stored in the cookie anymore;
    /// <c>CurrentUser</c> calls this on demand (per request) to resolve the user's
    /// permissions/roles for UI authorization decisions, keeping the cookie small.
    /// Returns empty on any parse failure rather than throwing.
    /// </summary>
    /// <param name="accessToken">The JWT access token to extract claims from.</param>
    /// <returns>
    /// An <see cref="IEnumerable{Claim}"/> containing all "role" and "Permission" claims found in the token,
    /// or an empty collection if the token cannot be parsed.
    /// </returns>
    internal static IEnumerable<Claim> ExtractUserClaimsFromJwt(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
                return [];

            var jwt = handler.ReadJwtToken(accessToken);

            return jwt.Claims
                .Where(c => c.Type == AppClaimTypes.Role
                         || c.Type == AppClaimTypes.Permission)
                .ToList();
        }
        catch
        {
            // If the token cannot be parsed for any reason, return no user claims.
            // The user is still authenticated via cookie; they simply have no permissions
            // visible on the Web side until they sign in again.
            return [];
        }
    }
}
