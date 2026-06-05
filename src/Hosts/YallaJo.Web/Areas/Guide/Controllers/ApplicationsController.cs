using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Applications;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class ApplicationsController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideApplicationsFacade _applications;

    public ApplicationsController(GuideApplicationsFacade applications) => _applications = applications;

    [HttpGet("guide/applications")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        SetSidebar();

        var result = await _applications.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ApplicationsVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/applications/apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyForTourFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _applications.ApplyAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return await ReloadAsync(form, ct);
        }

        SetSuccess("Application submitted.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(ApplyForTourFormVm form, CancellationToken ct)
    {
        var result = await _applications.GetAsync(1, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result is { IsSuccess: true, Data: not null } ? result.Data : new ApplicationsVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "Applications";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
