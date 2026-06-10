using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;

namespace YallaJo.Web.Features.Navbar;

/// <summary>
/// Renders the current user's avatar image in the shared header. Self-contained:
/// fetches the profile server-side and falls back to the bundled placeholder on
/// any API failure, so it can be safely embedded in every layout. The profile
/// lookup is cached per request (the navbar shows the avatar twice).
/// </summary>
public sealed class NavbarAvatarViewComponent : ViewComponent
{
    private const string CacheKey = "__NavbarAvatarVm";

    private readonly ProfileFacade _profile;

    public NavbarAvatarViewComponent(ProfileFacade profile) => _profile = profile;

    public async Task<IViewComponentResult> InvokeAsync(string cssClass = "avatar-img rounded-2", string? alt = null)
    {
        if (HttpContext.Items[CacheKey] is not NavbarAvatarVm vm)
        {
            string? avatarUrl = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var result = await _profile.GetAsync(HttpContext.RequestAborted);
                if (result.IsSuccess && result.Data is not null)
                    avatarUrl = result.Data.AvatarUrl;
            }

            vm = new NavbarAvatarVm(avatarUrl);
            HttpContext.Items[CacheKey] = vm;
        }

        ViewData["CssClass"] = cssClass;
        ViewData["Alt"] = alt;
        return View(vm);
    }
}

/// <summary>Resolved avatar URL for the header; null means "use the placeholder".</summary>
public sealed record NavbarAvatarVm(string? AvatarUrl);
