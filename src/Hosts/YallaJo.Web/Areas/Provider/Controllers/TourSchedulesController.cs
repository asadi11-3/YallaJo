using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourSchedules;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourSchedulesController : ProviderTourResourceController
{
    private readonly ProviderTourSchedulesFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourSchedulesController(ProviderTourSchedulesFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/schedules ────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/schedules")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourScheduleOutcome.Ok => View(result.Data),
            TourScheduleOutcome.ForceSignOut => RedirectToLogin(),
            TourScheduleOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── GET /provider/tours/{id}/schedules/create ─────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/schedules/create")]
    public async Task<IActionResult> Create(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourSchedule.Create))
            return RedirectToStatus();

        var result = await _facade.GetCreateAsync(id, ct);
        return result.Outcome switch
        {
            TourScheduleOutcome.Ok => View(result.Form),
            TourScheduleOutcome.ForceSignOut => RedirectToLogin(),
            TourScheduleOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/schedules/create ────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/schedules/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid id, TourScheduleFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourSchedule.Create))
            return RedirectToStatus();

        vm.TourId = id;
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.CreateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourScheduleOutcome.Ok:
                SetSuccess("Schedule created.");
                return RedirectToAction(nameof(Index), new { id });
            case TourScheduleOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourScheduleOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourScheduleOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            case TourScheduleOutcome.NothingCreated:
                ModelState.AddModelError(string.Empty, result.Error ?? "No schedule was added.");
                return View(vm);
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View(vm);
        }
    }

    // ── GET /provider/tours/{id}/schedules/{scheduleId}/edit ───────────────────────
    [HttpGet("provider/tours/{id:guid}/schedules/{scheduleId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, Guid scheduleId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourSchedule.Update))
            return RedirectToStatus();

        var result = await _facade.GetEditAsync(id, scheduleId, ct);
        return result.Outcome switch
        {
            TourScheduleOutcome.Ok => View(result.Form),
            TourScheduleOutcome.ForceSignOut => RedirectToLogin(),
            TourScheduleOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error, id),
        };
    }

    // ── POST /provider/tours/{id}/schedules/{scheduleId}/edit ──────────────────────
    [HttpPost("provider/tours/{id:guid}/schedules/{scheduleId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid scheduleId, TourScheduleFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourSchedule.Update))
            return RedirectToStatus();

        vm.TourId = id;
        vm.ScheduleId = scheduleId;
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.UpdateAsync(id, scheduleId, vm, ct);
        switch (result.Outcome)
        {
            case TourScheduleOutcome.Ok:
                SetSuccess("Schedule saved.");
                return RedirectToAction(nameof(Index), new { id });
            case TourScheduleOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourScheduleOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourScheduleOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            case TourScheduleOutcome.Conflict:
                SetError(result.Error);
                return View(vm);
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View(vm);
        }
    }

    // ── POST /provider/tours/{id}/schedules/{scheduleId}/delete ────────────────────
    [HttpPost("provider/tours/{id:guid}/schedules/{scheduleId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid scheduleId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourSchedule.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, scheduleId, ct);
        if (result.Outcome == TourScheduleOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourScheduleOutcome.Ok)
            SetSuccess("Schedule deleted.");
        else
            SetError(result.Error ?? "Could not delete the schedule.");

        return RedirectToAction(nameof(Index), new { id });
    }
}
