using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.Claims;

namespace YallaJo.Web.Infrastructure.Authentication.SignIn;

/// <summary>
/// Establishes or tears down the web app's cookie-based authenticated session.
///
/// Token flow:
///   - On sign-in: encrypts access + refresh tokens into the cookie claims
///   - On sign-out: removes the cookie entirely
///
/// The cookie is HttpOnly + SameSite=Strict (configured in Program.cs),
/// so tokens are never readable by JavaScript.
/// </summary>
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

        var claims = new[]
        {
            new Claim(AppClaimTypes.UserId,                userId.ToString()),
            new Claim(AppClaimTypes.AccessToken,           accessToken),
            new Claim(AppClaimTypes.RefreshToken,          refreshToken),
            new Claim(AppClaimTypes.RefreshTokenExpiresAt, refreshTokenExpiresAt.ToString("O")),
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
}
