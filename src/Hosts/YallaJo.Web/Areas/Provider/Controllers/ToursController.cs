using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class ToursController : ProviderTourResourceController
{
    private readonly ProviderToursFacade _facade;
    private readonly ProviderPlacesFacade _placesFacade;
    private readonly ICurrentUser _currentUser;

    public ToursController(
        ProviderToursFacade facade,
        ProviderPlacesFacade placesFacade,
        ICurrentUser currentUser)
    {
        _facade = facade;
        _placesFacade = placesFacade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours ───────────────────────────────────────────────────────
    [HttpGet("provider/tours")]
    public async Task<IActionResult> Index(string? status, int page = 1, CancellationToken ct = default)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(status, page, ct);
        return result.Outcome switch
        {
            ProviderTourOutcome.Ok => View(result.Data),
            ProviderTourOutcome.ForceSignOut => RedirectToLogin(),
            ProviderTourOutcome.Forbidden => RedirectToStatus(),
            _ => IndexError(result.Error, status, page),
        };
    }

    // ── GET /provider/tours/create ────────────────────────────────────────────────
    [HttpGet("provider/tours/create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Create))
            return RedirectToStatus();

        var vm = new ProviderTourFormVm();
        if (await PopulatePlaceOptionsAsync(vm, ct) is { } signOut)
            return signOut;

        return View("Upsert", vm);
    }

    // ── POST /provider/tours/create ───────────────────────────────────────────────
    [HttpPost("provider/tours/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderTourFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Create))
            return RedirectToStatus();

        if (!ModelState.IsValid)
        {
            if (await PopulatePlaceOptionsAsync(vm, ct) is { } signOut) return signOut;
            return View("Upsert", vm);
        }

        var result = await _facade.CreateAsync(vm, ct);
        switch (result.Outcome)
        {
            case ProviderTourOutcome.Ok:
                SetSuccess("Listing created as a draft.");
                return RedirectToAction(nameof(Edit), new { id = result.TourId });
            case ProviderTourOutcome.ForceSignOut:
                return RedirectToLogin();
            case ProviderTourOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                if (await PopulatePlaceOptionsAsync(vm, ct) is { } so) return so;
                return View("Upsert", vm);
        }
    }

    // ── GET /provider/tours/{id}/edit ─────────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetEditAsync(id, ct);
        switch (result.Outcome)
        {
            case ProviderTourOutcome.Ok:
                if (await PopulatePlaceOptionsAsync(result.Form!, ct) is { } signOut) return signOut;
                return View("Upsert", result.Form);
            case ProviderTourOutcome.ForceSignOut:
                return RedirectToLogin();
            case ProviderTourOutcome.Forbidden:
                return RedirectToStatus();
            default:
                return NotFoundRedirect(result.Error, notFoundFallback: "Listing not found.");
        }
    }

    // ── POST /provider/tours/{id}/edit ────────────────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProviderTourFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Update))
            return RedirectToStatus();

        vm.TourId = id;
        if (!ModelState.IsValid)
        {
            if (await PopulatePlaceOptionsAsync(vm, ct) is { } signOut) return signOut;
            return View("Upsert", vm);
        }

        var result = await _facade.UpdateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case ProviderTourOutcome.Ok:
                SetSuccess("Listing saved.");
                return RedirectToAction(nameof(Edit), new { id });
            case ProviderTourOutcome.ForceSignOut:
                return RedirectToLogin();
            case ProviderTourOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case ProviderTourOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index));
            case ProviderTourOutcome.Conflict:
                SetError(result.Error);
                return RedirectToAction(nameof(Edit), new { id });
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                if (await PopulatePlaceOptionsAsync(vm, ct) is { } so) return so;
                return View("Upsert", vm);
        }
    }

    // ── POST /provider/tours/{id}/submit ──────────────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Submit))
            return RedirectToStatus();

        var result = await _facade.SubmitAsync(id, ct);
        if (result.Outcome == ProviderTourOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderTourOutcome.Ok)
            SetSuccess("Listing submitted for review.");
        else
            SetError(result.Error ?? "Could not submit the listing.");

        return RedirectToAction(nameof(Index));
    }

    // ── POST /provider/tours/{id}/archive ─────────────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Archive))
            return RedirectToStatus();

        var result = await _facade.ArchiveAsync(id, ct);
        if (result.Outcome == ProviderTourOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderTourOutcome.Ok)
            SetSuccess("Listing archived.");
        else
            SetError(result.Error ?? "Could not archive the listing.");

        return RedirectToAction(nameof(Index));
    }

    // ── POST /provider/tours/{id}/delete ──────────────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.DeleteOwn))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, ct);
        if (result.Outcome == ProviderTourOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderTourOutcome.Ok)
            SetSuccess("Listing deleted.");
        else
            SetError(result.Error ?? "Could not delete the listing.");

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<IActionResult?> PopulatePlaceOptionsAsync(ProviderTourFormVm vm, CancellationToken ct)
    {
        var result = await _placesFacade.GetPlaceOptionsAsync(ct);
        if (result.ForceSignOut)
            return RedirectToLogin();

        vm.PlaceOptions = result.Options;
        vm.PlaceOptionsLoadError = result.LoadFailed ? result.Error : null;
        return null;
    }

    private IActionResult IndexError(string? message, string? status, int page)
    {
        SetError(message);
        return View(ProviderToursMapper.EmptyIndex(status, page < 1 ? 1 : page, 20));
    }
}
