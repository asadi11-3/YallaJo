using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoMetadata;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.SeoMetadata.Read)]
public sealed class SeoMetadataController : BaseController
{
    private readonly SeoMetadataFacade _facade;

    public SeoMetadataController(SeoMetadataFacade facade) => _facade = facade;

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
            SetError("Please correct the highlighted fields.");
            return RedirectToAction(nameof(Index), new { entityType = form.EntityType, entityId = form.EntityId });
        }

        var result = await _facade.UpsertAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "SEO metadata saved.", "Could not save the SEO metadata.");
        return RedirectToAction(nameof(Index), new { entityType = form.EntityType, entityId = form.EntityId });
    }
}
