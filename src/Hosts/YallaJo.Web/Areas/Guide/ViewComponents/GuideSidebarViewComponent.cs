using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Services;

namespace YallaJo.Web.Areas.Guide.ViewComponents;

/// <summary>
/// Renders the Guide dashboard sidebar with the real avatar and display name
/// resolved once per request via <see cref="GuideIdAccessor"/> (no extra API
/// round-trips thanks to its per-request memoization).
/// </summary>
public sealed class GuideSidebarViewComponent : ViewComponent
{
    private readonly GuideIdAccessor _guideId;

    public GuideSidebarViewComponent(GuideIdAccessor guideId) => _guideId = guideId;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var identity = await _guideId.GetIdentityAsync(HttpContext.RequestAborted);

        var vm = new GuideSidebarViewModel(
            DisplayName: identity?.DisplayName is { Length: > 0 } name
                ? name
                : HttpContext.User.Identity?.Name,
            AvatarUrl: identity?.AvatarUrl);

        return View(vm);
    }
}

/// <summary>View model for the Guide sidebar component.</summary>
public sealed record GuideSidebarViewModel(string? DisplayName, string? AvatarUrl);
