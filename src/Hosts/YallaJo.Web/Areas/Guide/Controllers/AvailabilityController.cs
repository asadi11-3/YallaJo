using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Availability;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class AvailabilityController : GuideBaseController
{
    private readonly GuideAvailabilityFacade _availability;

    public AvailabilityController(GuideAvailabilityFacade availability) => _availability = availability;

    [HttpGet("guide/availability")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetNav("Availability");
        var result = await _availability.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AvailabilityVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/availability/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddAvailabilityBlockFormVm form, CancellationToken ct = default)
    {
        SetNav("Availability");

        if (form.EndDate < form.StartDate)
        {
            ModelState.AddModelError(nameof(form.EndDate), L["Guide.Validation.EndAfterStart"].Value);
        }

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(form, ct);
        }

        var result = await _availability.AddBlockAsync(form, ct);
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

        SetSuccess(L["Guide.Flash.AvailabilityAdded"]);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/availability/{blockId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid blockId, CancellationToken ct = default)
    {
        var result = await _availability.DeleteBlockAsync(blockId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, L["Guide.Flash.AvailabilityRemoved"]);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(AddAvailabilityBlockFormVm form, CancellationToken ct)
    {
        var result = await _availability.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        var vm = result.IsSuccess && result.Data is not null ? result.Data : new AvailabilityVm();
        vm.Form = form;
        return View(nameof(Index), vm);
    }
}
