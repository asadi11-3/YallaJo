using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Places;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Place.Read)]
public sealed class PlacesController : BaseController
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 50;

    private readonly PlacesFacade _facade;
    public PlacesController(PlacesFacade facade) => _facade = facade;

    // ── List ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        PlaceListFilterVm? filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        filter ??= new PlaceListFilterVm();

        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

        var result = await _facade.GetPlacesAsync(page, pageSize, filter, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess)
        {
            // Same-page (non-redirect) failure: keep ViewBag.Error so the
            // message renders against this very response. Matches the
            // established Admin/Tours Index convention (SetError targets the
            // post-redirect flash, which this branch does not perform).
            ViewBag.Error = result.Error;
            var fallback = new PlaceListVm
            {
                Filter   = filter,
                Page     = page,
                PageSize = pageSize,
            };
            return WantsAjax() ? PartialView("_PlacesResults", fallback) : View(fallback);
        }

        return WantsAjax() ? PartialView("_PlacesResults", result.Data) : View(result.Data);
    }

    // ── Details ──────────────────────────────────────────────────────────────
    [HttpGet("admin/places/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Place not found.");
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── Create ───────────────────────────────────────────────────────────────
    [HttpGet("admin/places/create")]
    [RequirePermission(WebPermission.Place.Create)]
    public IActionResult Create() => View(new CreatePlaceVm());

    [HttpPost("admin/places/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Create)]
    public async Task<IActionResult> Create(CreatePlaceVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Place created.");
            return RedirectToAction(nameof(Index));
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create place.");
        return View(vm);
    }

    // ── Edit ─────────────────────────────────────────────────────────────────
    [HttpGet("admin/places/{id:guid}/edit")]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetForEditAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Place not found.");
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/places/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Edit(Guid id, EditPlaceVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Place updated.");
            return RedirectToAction(nameof(Index));
        }

        if (ApplyValidationErrors(result))
            return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update place.");
        return View(vm);
    }

    // ── Delete (API enforces Permission.Place.DeleteOwn; admins satisfy it via
    //    the handler's admin-tier / Place.DeleteAny override) ──────────────────
    [HttpPost("admin/places/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.DeleteOwn)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Place deleted.", "Could not delete place.");
        return RedirectToAction(nameof(Index));
    }

    // ── Feature toggle ───────────────────────────────────────────────────────
    [HttpPost("admin/places/{id:guid}/feature")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Feature(Guid id, bool featured, CancellationToken ct)
    {
        var result = await _facade.FeatureAsync(id, featured, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(featured ? "Place featured." : "Place unfeatured.");
        else
            SetError(result.Error ?? "Failed.");
        return RedirectToAction(nameof(Index));
    }

    // ── Verify toggle ────────────────────────────────────────────────────────
    [HttpPost("admin/places/{id:guid}/verify")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Verify(Guid id, bool verified, CancellationToken ct)
    {
        var result = await _facade.VerifyAsync(id, verified, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(verified ? "Place verified." : "Place unverified.");
        else
            SetError(result.Error ?? "Failed.");
        return RedirectToAction(nameof(Index));
    }

    // ── §8.5: accessibility set (batch replace) ────────────────────────────────
    [HttpPost("admin/places/{id:guid}/accessibility")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityFeature.Update)]
    public async Task<IActionResult> SetAccessibility(
        Guid id, List<AccessibilityFeatureFormItem>? features, CancellationToken ct)
    {
        var result = await _facade.SetAccessibilityAsync(id, features ?? [], ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Accessibility features updated.");
        else
            SetError(result.Error ?? "Could not update accessibility features.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── §8.5: accessibility remove (single assignment row) ─────────────────────
    [HttpPost("admin/places/{id:guid}/accessibility/{assignmentId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityFeature.Delete)]
    public async Task<IActionResult> RemoveAccessibility(Guid id, Guid assignmentId, CancellationToken ct)
    {
        var result = await _facade.RemoveAccessibilityAssignmentAsync(assignmentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Accessibility feature removed.");
        else
            SetError(result.Error ?? "Could not remove the accessibility feature.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
