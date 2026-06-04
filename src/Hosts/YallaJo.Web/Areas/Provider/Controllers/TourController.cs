using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourController : BaseController
{
    private readonly TourFacade _tours;

    public TourController(TourFacade tours) => _tours = tours;

    [HttpGet("provider/listings/create")]
    public IActionResult Create()
    {
        SetSidebar();
        return View(new CreateTourVm());
    }

    [HttpPost("provider/listings/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTourVm form, CancellationToken ct = default)
    {
        SetSidebar();
        if (!ModelState.IsValid)
            return View(form);

        var result = await _tours.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result is not { IsSuccess: true, Data: not null })
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return View(form);
        }

        SetSuccess("Listing created. Add schedules, pricing and stops to finish it.");
        return RedirectToAction(nameof(Manage), new { id = result.Data.TourId });
    }

    [HttpGet("provider/listings/{id:guid}/manage")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _tours.GetManageAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result is not { IsSuccess: true, Data: not null })
        {
            if (result.IsNotFound)
                return NotFound();
            SetError(result.Error);
            return RedirectToAction("Index", "Listings");
        }

        return View(result.Data);
    }

    [HttpPost("provider/listings/{id:guid}/schedules")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddSchedule(Guid id, AddScheduleVm form, CancellationToken ct = default)
        => RunAsync(id, () => _tours.AddScheduleAsync(id, form, ct), "Schedule added.");

    [HttpPost("provider/listings/{id:guid}/schedules/{scheduleId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteSchedule(Guid id, Guid scheduleId, CancellationToken ct = default)
        => RunAsync(id, () => _tours.DeleteScheduleAsync(id, scheduleId, ct), "Schedule removed.");

    [HttpPost("provider/listings/{id:guid}/pricing")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddPricing(Guid id, AddPricingVm form, CancellationToken ct = default)
        => RunAsync(id, () => _tours.AddPricingAsync(id, form, ct), "Pricing tier added.");

    [HttpPost("provider/listings/{id:guid}/pricing/{tierId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeletePricing(Guid id, Guid tierId, CancellationToken ct = default)
        => RunAsync(id, () => _tours.DeletePricingAsync(id, tierId, ct), "Pricing tier removed.");

    [HttpPost("provider/listings/{id:guid}/waypoints")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddWaypoint(Guid id, AddWaypointVm form, CancellationToken ct = default)
        => RunAsync(id, () => _tours.AddWaypointAsync(id, form, ct), "Stop added.");

    [HttpPost("provider/listings/{id:guid}/waypoints/{waypointId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteWaypoint(Guid id, Guid waypointId, CancellationToken ct = default)
        => RunAsync(id, () => _tours.DeleteWaypointAsync(id, waypointId, ct), "Stop removed.");

    [HttpPost("provider/listings/{id:guid}/children-info")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UpdateChildrenInfo(Guid id, ChildrenInfoVm form, CancellationToken ct = default)
        => RunAsync(id, () => _tours.UpdateChildrenInfoAsync(id, form, ct), "Children information updated.");

    private async Task<IActionResult> RunAsync(Guid id, Func<Task<Infrastructure.Api.Contracts.ApiResult>> action, string success)
    {
        var result = await action();
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess(success);
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Manage), new { id });
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Listings";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };
    }
}
