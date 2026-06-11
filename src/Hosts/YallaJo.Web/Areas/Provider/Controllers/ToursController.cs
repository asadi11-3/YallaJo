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
            // AJAX listing swap (PE1: PRG/full-view fallback preserved without JS).
            ProviderTourOutcome.Ok => WantsAjax() ? PartialView("_ToursResults", result.Data) : View(result.Data),
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
                SetSuccess(L["Provider.Flash.ListingCreated"]);
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
                return NotFoundRedirect(result.Error, notFoundFallback: L["Provider.Flash.ListingNotFound"].Value);
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
                SetSuccess(L["Provider.Flash.ListingSaved"]);
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
            SetSuccess(L["Provider.Flash.ListingSubmitted"]);
        else
            SetError(result.Error ?? L["Provider.Flash.CouldNotSubmitListing"].Value);

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
            SetSuccess(L["Provider.Flash.ListingArchived"]);
        else
            SetError(result.Error ?? L["Provider.Flash.CouldNotArchiveListing"].Value);

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
            SetSuccess(L["Provider.Flash.ListingDeleted"]);
        else
            SetError(result.Error ?? L["Provider.Flash.CouldNotDeleteListing"].Value);

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// [Backend] B2 Web proxy — JSON suggestions for the async place combobox (F10/JS5).
    /// The browser never calls the API host directly; no-JS users keep the SSR select (PE1).
    /// </summary>
    [HttpGet("provider/tours/places-lookup")]
    public async Task<IActionResult> PlacesLookup(string? term, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn)) return Forbid();

        var items = await _placesFacade.LookupAsync(term, ct);
        return Json(items.Select(p => new { id = p.Id, name = p.Name, city = p.City }));
    }

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
        var vm = ProviderToursMapper.EmptyIndex(status, page < 1 ? 1 : page, 20);
        return WantsAjax() ? PartialView("_ToursResults", vm) : View(vm);
    }
}
