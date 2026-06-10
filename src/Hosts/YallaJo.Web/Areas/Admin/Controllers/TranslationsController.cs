using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Translations;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.TranslationCache.Read)]
public sealed class TranslationsController : BaseController
{
    private readonly TranslationsFacade _facade;
    public TranslationsController(TranslationsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(
        EntityTypeOption? entityType, Guid? entityId, CancellationToken ct)
    {
        if (entityType is null || entityId is null || entityId == Guid.Empty)
            return View(new TranslationListVm());

        var filter = new TranslationFilterVm
        {
            EntityType = entityType.Value,
            EntityId   = entityId.Value,
        };

        var result = await _facade.GetForEntityAsync(filter, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new TranslationListVm { Filter = filter, HasFilter = true });
        }
        return View(result.Data);
    }

    [HttpGet("admin/translations/on-demand")]
    [RequirePermission(WebPermission.TranslationCache.Create)]
    public IActionResult OnDemand() => View(new TranslateOnDemandVm());

    [HttpPost("admin/translations/on-demand")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Create)]
    public async Task<IActionResult> OnDemand(TranslateOnDemandVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.TranslateOnDemandAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess && result.Data is not null)
        {
            return View(result.Data);
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Translation failed.");

        return View(vm);
    }

    [HttpGet("admin/translations/{id:guid}/edit")]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public IActionResult Edit(
        Guid id, string original, string translated,
        EntityTypeOption entityType, Guid entityId)
    {
        ViewBag.EntityType = entityType;
        ViewBag.EntityId   = entityId;
        ViewBag.Original   = original;
        return View(new UpdateTranslationVm { Id = id, TranslatedText = translated });
    }

    [HttpPost("admin/translations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public async Task<IActionResult> Edit(
        Guid id, UpdateTranslationVm vm,
        EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
        {
            ViewBag.EntityType = entityType;
            ViewBag.EntityId   = entityId;
            return View(vm);
        }

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Translation updated.");
            return RedirectToAction(nameof(Index), new { entityType, entityId });
        }

        // Re-render the same edit view with field-level errors (UI-UX-F6); restore the
        // entity context the view needs.
        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update translation.");

        ViewBag.EntityType = entityType;
        ViewBag.EntityId   = entityId;
        return View(vm);
    }

    [HttpPost("admin/translations/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public async Task<IActionResult> Approve(
        Guid id, EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Translation approved.", "Could not approve translation.");

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    // §8.9 — backfill missing translations for all Tag or Specialization rows.
    [HttpPost("admin/translations/backfill")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Create)]
    public async Task<IActionResult> Backfill(string? entityKind, CancellationToken ct)
    {
        var result = await _facade.BackfillAsync(entityKind, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Translation backfill started.", "Could not start the backfill.");
        return RedirectToAction(nameof(Index));
    }

    // §8.9 — mark all auto-translated fields reviewed for an entity + language.
    [HttpPost("admin/translations/approve-batch")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public async Task<IActionResult> ApproveBatch(
        EntityTypeOption entityType, Guid entityId, string? languageCode, List<string>? fieldNames, CancellationToken ct)
    {
        var result = await _facade.ApproveBatchAsync(entityType.ToString(), entityId, languageCode, fieldNames, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Translations approved.", "Could not approve the translations.");
        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }
}
