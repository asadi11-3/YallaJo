using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Place.Read)]
public sealed class PlacesController : Controller
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new PlaceListVm
            {
                Filter   = filter,
                Page     = page,
                PageSize = pageSize,
            });
        }

        return View(result.Data);
    }

    // ── Details ──────────────────────────────────────────────────────────────
    [HttpGet("admin/places/{id:guid}/details")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Place not found.";
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Place created.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create place.");
        return View(vm);
    }

    // ── Edit ─────────────────────────────────────────────────────────────────
    [HttpGet("admin/places/{id:guid}/edit")]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetForEditAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Place not found.";
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Place updated.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update place.");
        return View(vm);
    }

    // ── Delete (API enforces Permission.Place.SoftDelete) ────────────────────
    [HttpPost("admin/places/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.SoftDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Place deleted." : result.Error ?? "Could not delete place.";
        return RedirectToAction(nameof(Index));
    }

    // ── Feature toggle ───────────────────────────────────────────────────────
    [HttpPost("admin/places/{id:guid}/feature")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Feature(Guid id, bool featured, CancellationToken ct)
    {
        var result = await _facade.FeatureAsync(id, featured, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? (featured ? "Place featured." : "Place unfeatured.")
                : result.Error ?? "Failed.";
        return RedirectToAction(nameof(Index));
    }

    // ── Verify toggle ────────────────────────────────────────────────────────
    [HttpPost("admin/places/{id:guid}/verify")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Place.Update)]
    public async Task<IActionResult> Verify(Guid id, bool verified, CancellationToken ct)
    {
        var result = await _facade.VerifyAsync(id, verified, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess
                ? (verified ? "Place verified." : "Place unverified.")
                : result.Error ?? "Failed.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("Index", "Login", new { area = "Auth" });
}
