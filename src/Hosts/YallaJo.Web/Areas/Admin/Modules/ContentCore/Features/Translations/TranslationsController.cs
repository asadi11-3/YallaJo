using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.TranslationCache.Read)]
public sealed class TranslationsController : Controller
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
        if (result.RequireSignOut) return RedirectToLogin();
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess && result.Data is not null)
        {
            return View(result.Data);
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Translation updated.";
            return RedirectToAction(nameof(Index), new { entityType, entityId });
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            ViewBag.EntityType = entityType;
            ViewBag.EntityId   = entityId;
            return View(vm);
        }

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
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Translation approved." : result.Error ?? "Could not approve translation.";

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("Index", "Login", new { area = "Auth" });
}
