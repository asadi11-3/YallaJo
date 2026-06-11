using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.EntityTags;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.EntityTag.Read)]
public sealed class EntityTagsController : BaseController
{
    private readonly EntityTagsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EntityTagsController(EntityTagsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? entityType = null, Guid? entityId = null, CancellationToken ct = default)
    {
        var result = await _facade.GetAsync(entityType, entityId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            return View(new EntityTagsVm
            {
                Query = new EntityTagsQueryVm
                {
                    EntityType = entityType ?? string.Empty,
                    EntityId = entityId ?? Guid.Empty
                }
            });
        }
        return View(result.Data);
    }

    [HttpPost("admin/entity-tags/assign")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityTag.Create)]
    public async Task<IActionResult> Assign(
        string entityType, Guid entityId, List<Guid> tagIds, CancellationToken ct)
    {
        var result = await _facade.AssignAsync(entityType, entityId, tagIds, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.EntityTags.Flash.Assigned"].Value, _localizer["Admin.EntityTags.Flash.AssignFailed"].Value);
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    [HttpPost("admin/entity-tags/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityTag.Delete)]
    public async Task<IActionResult> Remove(
        string entityType, Guid entityId, Guid tagId, CancellationToken ct)
    {
        var result = await _facade.RemoveAsync(entityType, entityId, tagId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.EntityTags.Flash.Removed"].Value, _localizer["Admin.EntityTags.Flash.RemoveFailed"].Value);
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }
}
