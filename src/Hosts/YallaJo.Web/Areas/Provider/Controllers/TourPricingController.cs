using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourPricing;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourPricingController : ProviderTourResourceController
{
    private readonly ProviderTourPricingFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourPricingController(ProviderTourPricingFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/pricing ──────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/pricing")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourPricingOutcome.Ok => View(result.Data),
            TourPricingOutcome.ForceSignOut => RedirectToLogin(),
            TourPricingOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── GET /provider/tours/{id}/pricing/create ───────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/pricing/create")]
    public async Task<IActionResult> Create(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourPricingTier.Create))
            return RedirectToStatus();

        var result = await _facade.GetCreateAsync(id, ct);
        return result.Outcome switch
        {
            TourPricingOutcome.Ok => View(result.Form),
            TourPricingOutcome.ForceSignOut => RedirectToLogin(),
            TourPricingOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/pricing/create ──────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/pricing/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid id, TourPricingFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourPricingTier.Create))
            return RedirectToStatus();

        vm.TourId = id;
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.CreateAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourPricingOutcome.Ok:
                SetSuccess("Pricing tier created.");
                return RedirectToAction(nameof(Index), new { id });
            case TourPricingOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourPricingOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourPricingOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View(vm);
        }
    }

    // ── GET /provider/tours/{id}/pricing/{tierId}/edit ────────────────────────────
    [HttpGet("provider/tours/{id:guid}/pricing/{tierId:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, Guid tierId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourPricingTier.Update))
            return RedirectToStatus();

        var result = await _facade.GetEditAsync(id, tierId, ct);
        return result.Outcome switch
        {
            TourPricingOutcome.Ok => View(result.Form),
            TourPricingOutcome.ForceSignOut => RedirectToLogin(),
            TourPricingOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error, id),
        };
    }

    // ── POST /provider/tours/{id}/pricing/{tierId}/edit ───────────────────────────
    [HttpPost("provider/tours/{id:guid}/pricing/{tierId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid tierId, TourPricingFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourPricingTier.Update))
            return RedirectToStatus();

        vm.TourId = id;
        vm.TierId = tierId;
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.UpdateAsync(id, tierId, vm, ct);
        switch (result.Outcome)
        {
            case TourPricingOutcome.Ok:
                SetSuccess("Pricing tier saved.");
                return RedirectToAction(nameof(Index), new { id });
            case TourPricingOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourPricingOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourPricingOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            case TourPricingOutcome.Conflict:
                // e.g. deactivating the last active Adult tier — keep the user on the form.
                SetError(result.Error);
                return View(vm);
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error);
                return View(vm);
        }
    }

    // ── POST /provider/tours/{id}/pricing/{tierId}/delete ─────────────────────────
    [HttpPost("provider/tours/{id:guid}/pricing/{tierId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid tierId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourPricingTier.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, tierId, ct);
        if (result.Outcome == TourPricingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourPricingOutcome.Ok)
            SetSuccess("Pricing tier deleted.");
        else
            SetError(result.Error ?? "Could not delete the pricing tier.");

        return RedirectToAction(nameof(Index), new { id });
    }
}
