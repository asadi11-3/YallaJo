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

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new MyToursVm());
        }

        return View(result.Data);
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
        return HandleMutation(result, tourId, "Schedule added.");
    }

    [HttpPost("guide/tours/{tourId:guid}/schedules/{scheduleId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(Guid tourId, Guid scheduleId, CancellationToken ct = default)
    {
        var result = await _facade.DeleteScheduleAsync(tourId, scheduleId, ct);
        return HandleMutation(result, tourId, "Schedule removed.");
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
        return HandleMutation(result, tourId, "Pricing tier added.");
    }

    [HttpPost("guide/tours/{tourId:guid}/pricing-tiers/{tierId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePricingTier(Guid tourId, Guid tierId, CancellationToken ct = default)
    {
        var result = await _facade.DeletePricingTierAsync(tourId, tierId, ct);
        return HandleMutation(result, tourId, "Pricing tier removed.");
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
        return HandleMutation(result, tourId, "Private tour enabled.");
    }

    [HttpPost("guide/tours/{tourId:guid}/private-tour/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisablePrivateTour(Guid tourId, CancellationToken ct = default)
    {
        var result = await _facade.DisablePrivateTourAsync(tourId, ct);
        return HandleMutation(result, tourId, "Private tour disabled.");
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

    /// <summary>PRG guard: flash the first ModelState error and bounce back to the offering page.</summary>
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
        SetError(firstError ?? "Please check the form and try again.");
        return RedirectToAction(nameof(Offering), new { tourId });
    }

    private IActionResult HandleMutation(ApiResult result, Guid tourId, string successMessage)
    {
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, successMessage);
        return RedirectToAction(nameof(Offering), new { tourId });
    }
}
