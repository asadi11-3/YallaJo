using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.MyTours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class MyToursController : GuideBaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideMyToursFacade _facade;

    public MyToursController(GuideMyToursFacade facade) => _facade = facade;

    [HttpGet("guide/tours")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        SetNav("MyTours");
        if (page < 1)
        {
            page = 1;
        }

        var result = await _facade.GetMyToursAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        MyToursVm vm;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            vm = new MyToursVm();
        }
        else
        {
            vm = result.Data;
        }

        if (WantsAjax())
        {
            return PartialView("_MyToursResults", vm);
        }

        return View(vm);
    }

    [HttpGet("guide/tours/{tourId:guid}")]
    public async Task<IActionResult> Offering(Guid tourId, CancellationToken ct = default)
    {
        SetNav("MyTours");
        var result = await _facade.GetOfferingDetailAsync(tourId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("guide/tours/{tourId:guid}/schedules")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSchedule(Guid tourId, AddScheduleFormVm form, CancellationToken ct = default)
    {
        if (InvalidModelRedirect(tourId) is { } invalid)
        {
            return invalid;
        }

        var result = await _facade.AddScheduleAsync(tourId, form, ct);
        return await HandleMutationAsync(result, tourId, "Schedule added.", "_OfferingSchedules", ct);
    }

    [HttpPost("guide/tours/{tourId:guid}/schedules/{scheduleId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(Guid tourId, Guid scheduleId, CancellationToken ct = default)
    {
        var result = await _facade.DeleteScheduleAsync(tourId, scheduleId, ct);
        return await HandleMutationAsync(result, tourId, "Schedule removed.", "_OfferingSchedules", ct);
    }

    [HttpPost("guide/tours/{tourId:guid}/pricing-tiers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPricingTier(Guid tourId, AddPricingTierFormVm form, CancellationToken ct = default)
    {
        if (InvalidModelRedirect(tourId) is { } invalid)
        {
            return invalid;
        }

        var result = await _facade.AddPricingTierAsync(tourId, form, ct);
        return await HandleMutationAsync(result, tourId, "Pricing tier added.", "_OfferingPricingTiers", ct);
    }

    [HttpPost("guide/tours/{tourId:guid}/pricing-tiers/{tierId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePricingTier(Guid tourId, Guid tierId, CancellationToken ct = default)
    {
        var result = await _facade.DeletePricingTierAsync(tourId, tierId, ct);
        return await HandleMutationAsync(result, tourId, "Pricing tier removed.", "_OfferingPricingTiers", ct);
    }

    [HttpPost("guide/tours/{tourId:guid}/private-tour")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnablePrivateTour(Guid tourId, PrivateTourFormVm form, CancellationToken ct = default)
    {
        if (InvalidModelRedirect(tourId) is { } invalid)
        {
            return invalid;
        }

        var result = await _facade.EnablePrivateTourAsync(tourId, form, ct);
        return await HandleMutationAsync(result, tourId, "Private tour enabled.", "_OfferingPrivateTour", ct);
    }

    [HttpPost("guide/tours/{tourId:guid}/private-tour/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisablePrivateTour(Guid tourId, CancellationToken ct = default)
    {
        var result = await _facade.DisablePrivateTourAsync(tourId, ct);
        return await HandleMutationAsync(result, tourId, "Private tour disabled.", "_OfferingPrivateTour", ct);
    }

    // POST /guide/tours/{tourId}/offering/remove — remove the guide's whole offering on this tour.
    [HttpPost("guide/tours/{tourId:guid}/offering/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideOffering.Delete)]
    public async Task<IActionResult> RemoveOffering(Guid tourId, CancellationToken ct = default)
    {
        var result = await _facade.RemoveOfferingAsync(tourId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            SetSuccess("Offering removed.");
        }
        else
        {
            SetError(result.Error ?? "Could not remove the offering.");
        }

        // The offering no longer exists — return to the tours list, not the offering page.
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Guard for invalid ModelState. AJAX (WantsAjax): return the first error as
    /// <c>400 { error }</c> so provider-actions.js toasts it. Otherwise PRG: flash the
    /// first ModelState error and bounce back to the offering page.
    /// </summary>
    private IActionResult? InvalidModelRedirect(Guid tourId)
    {
        if (ModelState.IsValid)
        {
            return null;
        }

        var firstError = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        if (WantsAjax())
        {
            return BadRequest(new { error = firstError ?? "Please check the form and try again." });
        }

        SetError(firstError ?? "Please check the form and try again.");
        return RedirectToAction(nameof(Offering), new { tourId });
    }

    /// <summary>
    /// Shared epilogue for offering mutations. AJAX (WantsAjax): on failure return
    /// <c>400 { error }</c> (toasted by provider-actions.js); on success re-fetch the
    /// offering and return the swappable fragment named by <paramref name="fragmentName"/>
    /// so the client replaces just that card (PE1). Otherwise: flash + PRG redirect.
    /// </summary>
    private async Task<IActionResult> HandleMutationAsync(
        ApiResult result, Guid tourId, string successMessage, string fragmentName, CancellationToken ct)
    {
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Message ?? "The action could not be completed." });
            }

            var refreshed = await _facade.GetOfferingDetailAsync(tourId, ct);
            if (GuardSignOut(refreshed) is { } so)
            {
                return so;
            }

            return PartialView(fragmentName, refreshed.Data ?? new OfferingDetailVm());
        }

        SetFlash(result, successMessage);
        return RedirectToAction(nameof(Offering), new { tourId });
    }
}
