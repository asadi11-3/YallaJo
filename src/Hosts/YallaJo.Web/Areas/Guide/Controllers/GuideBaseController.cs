using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

/// <summary>
/// Base controller for the Guide dashboard area.
/// Centralizes the [Area]/[Authorize] plumbing previously copy-pasted across 14 controllers and
/// applies NoStore to every response — all Guide pages render per-user authenticated data (UI-PERF-C2).
/// </summary>
[Area("Guide")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class GuideBaseController : BaseController
{
    /// <summary>Sets the active sidebar nav item consumed by the GuideSidebar view component.</summary>
    protected void SetNav(string key) => ViewData["GuideNav"] = key;
}
