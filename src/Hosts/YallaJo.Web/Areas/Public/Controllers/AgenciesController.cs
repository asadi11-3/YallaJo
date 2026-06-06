using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Agencies;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class AgenciesController(AgenciesFacade agencies) : BaseController
{
    [HttpGet("agency")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await agencies.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AgenciesGridVm());
        }

        return View(result.Data);
    }

    [HttpGet("agency/{agencyUserId:guid}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Detail(Guid agencyUserId, CancellationToken ct = default)
    {
        var result = await agencies.GetDetailAsync(agencyUserId, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpPost("agency/{agencyUserId:guid}/apply")]
    [Authorize(Roles = "TourGuide")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(Guid agencyUserId, CancellationToken ct = default)
    {
        var result = await agencies.ApplyAsync(agencyUserId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            SetSuccess("Your agency application has been submitted.");
        }
        else
        {
            SetError(result.Error);
        }

        return RedirectToAction(nameof(Detail), new { agencyUserId });
    }
}
