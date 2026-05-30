using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Category.Read)]
public sealed class CategoriesController : Controller
{
    private readonly CategoriesFacade _facade;
    public CategoriesController(CategoriesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetCategoriesAsync(includeInactive: true, ct);
        if (result.RequireSignOut) return RedirectToLogin();
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

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
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Category not found.";
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
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update category.");
        return View(vm);
    }

    [HttpPost("admin/categories/{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Category deactivated." : result.Error ?? "Failed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/categories/{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Update)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ActivateAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Category activated." : result.Error ?? "Failed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/categories/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Category.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Category deleted." : result.Error ?? "Failed.";
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

    private IActionResult RedirectToLogin()
        => RedirectToAction("Index", "Login", new { area = "Auth" });
}
