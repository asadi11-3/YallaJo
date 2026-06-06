using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class CultureController : BaseController
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase) { "ar", "en" };

    [HttpPost("set-language")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SetLanguage(string culture, string? returnUrl = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(culture) || !Supported.Contains(culture))
        {
            culture = "ar";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = true,
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Task.FromResult<IActionResult>(LocalRedirect(returnUrl));
        }

        return Task.FromResult<IActionResult>(LocalRedirect("/"));
    }
}
