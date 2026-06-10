using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Categories;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Category.Read)]
public sealed class CategoriesController : BaseController
{
    private readonly CategoriesFacade _facade;
    public CategoriesController(CategoriesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetCategoriesAsync(includeInactive: true, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new CategoryListVm());
        }
        return View(result.Data);
    }

    [HttpPost("admin/categories/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Create)]
    public async Task<IActionResult> Create(CreateCategoryVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndex(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Category created.");
            return RedirectToAction(nameof(Index));
        }

        // The create form is rendered as a sub-section of the Index view and binds its
        // fields under the "Create." prefix, so API field errors must be prefixed to
        // surface against the right inputs (BaseController.ApplyValidationErrors emits
        // bare field names, which would not match here).
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Create.{field}", m);
            return await ReloadIndex(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create category.");
        return await ReloadIndex(vm, ct);
    }

    [HttpGet("admin/categories/{id:guid}/edit")]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetForEditAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Category not found.");
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpPost("admin/categories/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Edit(Guid id, UpdateCategoryVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
        {
            // F10: repopulate the parent-name dropdown options before re-rendering.
            vm.ParentOptions = await _facade.LoadParentOptionsAsync(id, ct);
            return View(vm);
        }

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Category updated.");
            return RedirectToAction(nameof(Index));
        }

        // Re-render the same edit view with field-level errors (UI-UX-F6) — repopulate
        // the parent-name dropdown first so it survives the round-trip.
        vm.ParentOptions = await _facade.LoadParentOptionsAsync(id, ct);

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update category.");

        return View(vm);
    }

    [HttpPost("admin/categories/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Category deactivated.", "Failed.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/categories/{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ActivateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Category activated.", "Failed.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/categories/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Category deleted.", "Failed.");
        return RedirectToAction(nameof(Index));
    }

    // §8.9 — restore a soft-deleted category.
    [HttpPost("admin/categories/{id:guid}/restore")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await _facade.RestoreAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Category restored.", "Failed.");
        return RedirectToAction(nameof(Index));
    }

    // §8.9 — batch reorder categories.
    [HttpPost("admin/categories/reorder")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Reorder(List<Guid> ids, List<int> sortOrders, CancellationToken ct)
    {
        var result = await _facade.ReorderAsync(ids, sortOrders, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Categories reordered.", "Failed.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadIndex(CreateCategoryVm create, CancellationToken ct)
    {
        var list = await _facade.GetCategoriesAsync(includeInactive: true, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new CategoryListVm { Categories = list.Data.Categories, Create = create }
            : new CategoryListVm { Create = create };
        return View(nameof(Index), vm);
    }
}
