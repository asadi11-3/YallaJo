using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoMetadata;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.SeoMetadata.Read)]
public sealed class SeoMetadataController : BaseController
{
    private readonly SeoMetadataFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SeoMetadataController(SeoMetadataFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] SeoMetadataLookupRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "SeoMetadata";
        var result = await _facade.GetAsync(request.EntityType, request.EntityId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new SeoMetadataVm { EntityType = request.EntityType, EntityId = request.EntityId });
        }

        return View(result.Data);
    }

    [HttpPost("admin/seo/metadata/save")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SeoMetadata.Create)]
    public async Task<IActionResult> Save(SeoMetadataFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Admin.SeoMetadata.Flash.FixFields"].Value);
            return RedirectToAction(nameof(Index), new { entityType = form.EntityType, entityId = form.EntityId });
        }

        var result = await _facade.UpsertAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, _localizer["Admin.SeoMetadata.Flash.Saved"].Value, _localizer["Admin.SeoMetadata.Flash.SaveFailed"].Value);
        return RedirectToAction(nameof(Index), new { entityType = form.EntityType, entityId = form.EntityId });
    }

    // ── §8.10: soft-delete SEO metadata ─────────────────────────────────────────
    [HttpPost("admin/seo/metadata/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SeoMetadata.Delete)]
    public async Task<IActionResult> Delete(Guid id, SeoEntityType entityType, Guid? entityId, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.SeoMetadata.Flash.Deleted"].Value, _localizer["Admin.SeoMetadata.Flash.DeleteFailed"].Value);
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }
}
