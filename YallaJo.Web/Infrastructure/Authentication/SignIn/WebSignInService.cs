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

        // Core token-storage claims.
        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId,                userId.ToString()),
            new(AppClaimTypes.AccessToken,           accessToken),
            new(AppClaimTypes.RefreshToken,          refreshToken),
            new(AppClaimTypes.RefreshTokenExpiresAt, refreshTokenExpiresAt.ToString("O")),
        };

        // Extract role + Permission claims from the JWT so that controllers and
        // Razor views can check user permissions without re-parsing the token.
        // No signature verification — we trust our own freshly-issued token.
        claims.AddRange(ExtractUserClaimsFromJwt(accessToken));

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
    /// all "role" and "Permission" claims so they can be stored in the Web cookie.
    /// Returns empty on any parse failure rather than throwing.
    /// </summary>
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
