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

    // TempData keys carrying the on-demand translation result across the PRG redirect.
    // TempData survives exactly one request then auto-evicts, so a browser refresh of the
    // GET result page does NOT re-POST and therefore never re-bills the translation provider.
    private const string OnDemandOriginalKey   = "Translations.OnDemand.Original";
    private const string OnDemandTranslatedKey  = "Translations.OnDemand.Translated";
    private const string OnDemandConfidenceKey  = "Translations.OnDemand.Confidence";

    [HttpGet("admin/translations/on-demand")]
    [RequirePermission(WebPermission.TranslationCache.Create)]
    public IActionResult OnDemand()
    {
        // Read back the result of a prior successful POST (PRG), if any.
        var vm = new TranslateOnDemandVm
        {
            LastOriginal   = TempData[OnDemandOriginalKey] as string,
            LastTranslated = TempData[OnDemandTranslatedKey] as string,
            LastConfidence = TempData[OnDemandConfidenceKey] is double c ? c : null,
        };
        return View(vm);
    }

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
            // PRG: stash the (small) result in TempData and redirect to the GET page so a
            // refresh re-issues a harmless GET instead of re-running the paid translation.
            TempData[OnDemandOriginalKey]   = result.Data.LastOriginal;
            TempData[OnDemandTranslatedKey]  = result.Data.LastTranslated;
            if (result.Data.LastConfidence is { } confidence)
                TempData[OnDemandConfidenceKey] = confidence;
            return RedirectToAction(nameof(OnDemand));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Translation failed.");

        return View(vm);
    }

    [HttpGet("admin/translations/{id:guid}/edit")]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public async Task<IActionResult> Edit(
        Guid id, string original, string translated,
        EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        // PE1: deep links render the Index with the edit modal server-side open.
        return await IndexWithEditModal(
            new UpdateTranslationVm { Id = id, TranslatedText = translated },
            entityType, entityId, original, ct);
    }

    [HttpPost("admin/translations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TranslationCache.Update)]
    public async Task<IActionResult> Edit(
        Guid id, [Bind(Prefix = "Edit")] UpdateTranslationVm vm,
        EntityTypeOption entityType, Guid entityId, string? original, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
            return await IndexWithEditModal(vm, entityType, entityId, original, ct);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Translation updated.");
            return RedirectToAction(nameof(Index), new { entityType, entityId });
        }

        if (result.ValidationErrors is not null)
        {
            // UI-UX-F6: map API field errors to the Edit-prefixed modal inputs.
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Edit.{field}", m);
            return await IndexWithEditModal(vm, entityType, entityId, original, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update translation.");
        return await IndexWithEditModal(vm, entityType, entityId, original, ct);
    }

    // Renders the Index with the entity context loaded and the edit modal open (F8 §4.7).
    private async Task<IActionResult> IndexWithEditModal(
        UpdateTranslationVm edit, EntityTypeOption entityType, Guid entityId,
        string? original, CancellationToken ct)
    {
        var filter = new TranslationFilterVm { EntityType = entityType, EntityId = entityId };
        var list = await _facade.GetForEntityAsync(filter, ct);
        var vm = new TranslationListVm
        {
            Filter = filter,
            HasFilter = true,
            Translations = list.IsSuccess && list.Data is not null ? list.Data.Translations : [],
            EditId = edit.Id,
            Edit = edit,
            EditOriginal = original ?? string.Empty,
        };
        return View(nameof(Index), vm);
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
