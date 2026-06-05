using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.EntityCategories;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.EntityCategory.Read)]
public sealed class EntityCategoriesController : BaseController
{
    private readonly EntityCategoriesFacade _facade;

    public EntityCategoriesController(EntityCategoriesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(string? entityType = null, Guid? entityId = null, CancellationToken ct = default)
    {
        var result = await _facade.GetAsync(entityType, entityId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            return View(new EntityCategoriesVm
            {
                Query = new EntityCategoriesQueryVm
                {
                    EntityType = entityType ?? "",
                    EntityId = entityId ?? Guid.Empty
                }
            });
        }
        return View(result.Data);
    }

    [HttpPost("admin/entity-categories/assign")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityCategory.Create)]
    public async Task<IActionResult> Assign(string entityType, Guid entityId, List<Guid> categoryIds, CancellationToken ct = default)
    {
        var result = await _facade.AssignAsync(entityType, entityId, categoryIds, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;
        SetFlash(result, "Categories assigned.", "Could not assign categories.");
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    [HttpPost("admin/entity-categories/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityCategory.Delete)]
    public async Task<IActionResult> Remove(string entityType, Guid entityId, Guid categoryId, CancellationToken ct = default)
    {
        var result = await _facade.RemoveAsync(entityType, entityId, categoryId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;
        SetFlash(result, "Category removed.", "Could not remove category.");
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }
}
