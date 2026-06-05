using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.MyTours;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class MyToursController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideMyToursFacade _facade;

    public MyToursController(GuideMyToursFacade facade) => _facade = facade;

    [HttpGet("guide/tours")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        SetSidebar();
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
        SetSidebar();
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

    private IActionResult HandleMutation(ApiResult result, Guid tourId, string successMessage)
    {
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, successMessage);
        return RedirectToAction(nameof(Offering), new { tourId });
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "MyTours";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
