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
