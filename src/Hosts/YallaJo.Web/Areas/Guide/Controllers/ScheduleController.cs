using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class ScheduleController : BaseController
{
    private readonly GuideScheduleFacade _schedule;

    public ScheduleController(GuideScheduleFacade schedule) => _schedule = schedule;

    [HttpGet("guide/schedule")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _schedule.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GuideScheduleVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/schedule/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddBlockVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please provide a valid date range.");
            return RedirectToAction(nameof(Index));
        }

        if (form.EndDate < form.StartDate)
        {
            SetError("The end date cannot be before the start date.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _schedule.AddAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            SetSuccess("Unavailable dates added.");
        }
        else
        {
            SetError(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/schedule/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _schedule.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            SetSuccess("Block removed.");
        }
        else
        {
            SetError(result.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Schedule";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
