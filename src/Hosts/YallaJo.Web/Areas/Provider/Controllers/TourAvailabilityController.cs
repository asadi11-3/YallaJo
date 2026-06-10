using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourAvailability;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourAvailabilityController : ProviderTourResourceController
{
    private readonly ProviderTourAvailabilityFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourAvailabilityController(ProviderTourAvailabilityFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/availability ─────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/availability")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Read))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourAvailabilityOutcome.Ok => View(result.Data),
            TourAvailabilityOutcome.ForceSignOut => RedirectToLogin(),
            TourAvailabilityOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/availability/bulk ───────────────────────────────
    [HttpPost("provider/tours/{id:guid}/availability/bulk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(Guid id, BulkAvailabilitySlotFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Create))
            return RedirectToStatus();

        vm.TourId = id;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the recurring-slots form correctly.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.BulkCreateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourAvailabilityOutcome.Ok:
                SetSuccess("Recurring availability slots created.");
                return RedirectToAction(nameof(Index), new { id });
            case TourAvailabilityOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourAvailabilityOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            default:
                SetError(result.Error ?? "Could not create the recurring slots.");
                return RedirectToAction(nameof(Index), new { id });
        }
    }

    // ── GET /provider/tours/{id}/availability/create ──────────────────────────────
    [HttpGet("provider/tours/{id:guid}/availability/create")]
    public async Task<IActionResult> Create(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Create))
            return RedirectToStatus();

        var result = await _facade.GetCreateAsync(id, ct);
        return result.Outcome switch
        {
            TourAvailabilityOutcome.Ok => View("Upsert", result.Form),
            TourAvailabilityOutcome.ForceSignOut => RedirectToLogin(),
            TourAvailabilityOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/availability/create ─────────────────────────────
    [HttpPost("provider/tours/{id:guid}/availability/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid id, AvailabilitySlotFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Create))
            return RedirectToStatus();

        vm.TourId = id;
        vm.IsEdit = false;
        if (!ModelState.IsValid)
            return View("Upsert", vm);

        var result = await _facade.CreateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourAvailabilityOutcome.Ok:
                SetSuccess("Availability slot created.");
                return RedirectToAction(nameof(Index), new { id });
            case TourAvailabilityOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourAvailabilityOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourAvailabilityOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            case TourAvailabilityOutcome.Conflict:
                ModelState.AddModelError(string.Empty, result.Error ?? "This slot conflicts with an existing one.");
                return View("Upsert", vm);
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View("Upsert", vm);
        }
    }

    // ── GET /provider/tours/{id}/availability/{slotId}/edit ───────────────────────
    [HttpGet("provider/tours/{id:guid}/availability/{slotId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, Guid slotId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Update))
            return RedirectToStatus();

        var result = await _facade.GetEditAsync(id, slotId, ct);
        return result.Outcome switch
        {
            TourAvailabilityOutcome.Ok => View("Upsert", result.Form),
            TourAvailabilityOutcome.ForceSignOut => RedirectToLogin(),
            TourAvailabilityOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error, id),
        };
    }

    // ── POST /provider/tours/{id}/availability/{slotId}/edit ──────────────────────
    [HttpPost("provider/tours/{id:guid}/availability/{slotId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid slotId, AvailabilitySlotFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Update))
            return RedirectToStatus();

        vm.TourId = id;
        vm.SlotId = slotId;
        vm.IsEdit = true;

        // Only capacity is editable; date/time aren't validated for edit.
        ModelState.Remove(nameof(vm.Date));
        ModelState.Remove(nameof(vm.StartTime));
        ModelState.Remove(nameof(vm.EndTime));
        if (vm.MaxCapacity < 1)
            ModelState.AddModelError(nameof(vm.MaxCapacity), "Capacity must be at least 1.");

        if (!ModelState.IsValid)
            return View("Upsert", vm);

        var result = await _facade.UpdateAsync(id, slotId, vm, ct);
        switch (result.Outcome)
        {
            case TourAvailabilityOutcome.Ok:
                SetSuccess("Availability slot saved.");
                return RedirectToAction(nameof(Index), new { id });
            case TourAvailabilityOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourAvailabilityOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourAvailabilityOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            case TourAvailabilityOutcome.Conflict:
                SetError(result.Error);
                return View("Upsert", vm);
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View("Upsert", vm);
        }
    }

    // ── POST /provider/tours/{id}/availability/{slotId}/delete ────────────────────
    [HttpPost("provider/tours/{id:guid}/availability/{slotId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid slotId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.AvailabilitySlot.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, slotId, ct);
        if (result.Outcome == TourAvailabilityOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourAvailabilityOutcome.Ok)
            SetSuccess("Availability slot removed.");
        else
            SetError(result.Error ?? "Could not remove the slot.");

        return RedirectToAction(nameof(Index), new { id });
    }
}
