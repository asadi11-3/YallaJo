using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourWaypoints;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourWaypointsController : ProviderTourResourceController
{
    private readonly ProviderTourWaypointsFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourWaypointsController(ProviderTourWaypointsFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/waypoints ────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/waypoints")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourWaypointOutcome.Ok => View(result.Data),
            TourWaypointOutcome.ForceSignOut => RedirectToLogin(),
            TourWaypointOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── GET /provider/tours/{id}/waypoints/create ─────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/waypoints/create")]
    public async Task<IActionResult> Create(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Create))
            return RedirectToStatus();

        var result = await _facade.GetCreateAsync(id, ct);
        return result.Outcome switch
        {
            TourWaypointOutcome.Ok => View("Upsert", result.Form),
            TourWaypointOutcome.ForceSignOut => RedirectToLogin(),
            TourWaypointOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/waypoints/create ────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/waypoints/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid id, TourWaypointFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Create))
            return RedirectToStatus();

        vm.TourId = id;
        if (!ModelState.IsValid)
            return View("Upsert", vm);

        var result = await _facade.CreateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourWaypointOutcome.Ok:
                SetSuccess("Waypoint added.");
                return RedirectToAction(nameof(Index), new { id });
            case TourWaypointOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourWaypointOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourWaypointOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View("Upsert", vm);
        }
    }

    // ── GET /provider/tours/{id}/waypoints/{waypointId}/edit ──────────────────────
    [HttpGet("provider/tours/{id:guid}/waypoints/{waypointId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, Guid waypointId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Update))
            return RedirectToStatus();

        var result = await _facade.GetEditAsync(id, waypointId, ct);
        return result.Outcome switch
        {
            TourWaypointOutcome.Ok => View("Upsert", result.Form),
            TourWaypointOutcome.ForceSignOut => RedirectToLogin(),
            TourWaypointOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error, id),
        };
    }

    // ── POST /provider/tours/{id}/waypoints/{waypointId}/edit ─────────────────────
    [HttpPost("provider/tours/{id:guid}/waypoints/{waypointId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid waypointId, TourWaypointFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Update))
            return RedirectToStatus();

        vm.TourId = id;
        vm.WaypointId = waypointId;
        if (!ModelState.IsValid)
            return View("Upsert", vm);

        var result = await _facade.UpdateAsync(id, waypointId, vm, ct);
        switch (result.Outcome)
        {
            case TourWaypointOutcome.Ok:
                SetSuccess("Waypoint saved.");
                return RedirectToAction(nameof(Index), new { id });
            case TourWaypointOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourWaypointOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourWaypointOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View("Upsert", vm);
        }
    }

    // ── POST /provider/tours/{id}/waypoints/{waypointId}/delete ───────────────────
    [HttpPost("provider/tours/{id:guid}/waypoints/{waypointId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid waypointId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, waypointId, ct);
        if (result.Outcome == TourWaypointOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourWaypointOutcome.Ok)
            SetSuccess("Waypoint deleted.");
        else
            SetError(result.Error ?? "Could not delete the waypoint.");

        return RedirectToAction(nameof(Index), new { id });
    }

    // ── POST /provider/tours/{id}/waypoints/reorder ───────────────────────────────
    [HttpPost("provider/tours/{id:guid}/waypoints/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid id, List<Guid> waypointIds, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourWaypoint.Update))
            return RedirectToStatus();

        if (waypointIds is not { Count: > 0 })
        {
            SetError("No waypoint order was submitted.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.ReorderAsync(id, waypointIds, ct);
        if (result.Outcome == TourWaypointOutcome.ForceSignOut) return RedirectToLogin();

        // NF6: drag-and-drop posts via AJAX and only needs a status — the client
        // already moved the row optimistically and rolls back on failure.
        if (WantsAjax())
        {
            return result.Outcome == TourWaypointOutcome.Ok
                ? Ok()
                : BadRequest(new { error = result.Error ?? "Could not reorder the waypoints." });
        }

        if (result.Outcome == TourWaypointOutcome.Ok)
            SetSuccess("Route order updated.");
        else
            SetError(result.Error ?? "Could not reorder the waypoints.");

        return RedirectToAction(nameof(Index), new { id });
    }
}
