using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

/// <summary>
/// Phase 3 view reduction: the Tier page was merged into the Dashboard
/// (rendered by the shared _TierProgress partial at the #tier anchor).
/// The route is kept so existing bookmarks/deep links 301 to the new home.
/// </summary>
[RequirePermission(WebPermission.GuideDashboard.Read)]
public sealed class TierController : GuideBaseController
{
    [HttpGet("guide/tier")]
    public IActionResult Index() => RedirectPermanent("/guide/dashboard#tier");
}
