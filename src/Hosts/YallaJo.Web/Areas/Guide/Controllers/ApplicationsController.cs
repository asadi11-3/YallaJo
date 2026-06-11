using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Applications;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class ApplicationsController : GuideBaseController
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

        SetNav("Applications");

        var result = await _applications.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        ApplicationsVm vm;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            vm = new ApplicationsVm();
        }
        else
        {
            vm = result.Data;
        }

        if (WantsAjax())
        {
            return PartialView("_ApplicationsResults", vm);
        }

        return View(vm);
    }

    /// <summary>
    /// [Backend] B2 Web proxy — JSON options for the async open-tour picker (F10/JS5).
    /// The browser never calls the API host directly; no-JS users keep the SSR select (PE1).
    /// </summary>
    [HttpGet("guide/applications/open-tours")]
    public async Task<IActionResult> OpenTours(string? q, CancellationToken ct = default)
    {
        var result = await _applications.GetOpenToursAsync(q, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var items = result.Data ?? [];
        return Json(items.Select(t => new { tourId = t.TourId, label = t.Label }));
    }

    [HttpPost("guide/applications/apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyForTourFormVm form, CancellationToken ct = default)
    {
        SetNav("Applications");

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
}
