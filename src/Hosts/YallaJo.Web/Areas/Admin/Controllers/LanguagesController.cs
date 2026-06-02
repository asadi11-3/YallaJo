using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Infrastructure.Authorization;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Language.Read)]
public sealed class LanguagesController : Controller
{
    private readonly LanguagesFacade _facade;
    public LanguagesController(LanguagesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetLanguagesAsync(activeOnly, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new LanguageListVm { ActiveOnly = activeOnly });
        }
        return View(result.Data);
    }

    [HttpPost("admin/languages/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Language.Create)]
    public async Task<IActionResult> Create(CreateLanguageVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndex(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Language created.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Create.{field}", m);
            return await ReloadIndex(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create language.");
        return await ReloadIndex(vm, ct);
    }

    [HttpGet("admin/languages/{id:guid}/edit")]
    [RequirePermission(WebPermission.Language.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        if (list.RequireSignOut) return RedirectToLogin();
        if (!list.IsSuccess || list.Data is null)
        {
            TempData["Error"] = list.Error ?? "Could not load languages.";
            return RedirectToAction(nameof(Index));
        }

        var row = list.Data.Languages.FirstOrDefault(l => l.Id == id);
        if (row is null)
        {
            TempData["Error"] = "Language not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(new UpdateLanguageVm
        {
            Id         = row.Id,
            Name       = row.Name,
            NativeName = row.NativeName,
            IsRtl      = row.IsRtl,
            IsActive   = row.IsActive,
        });
    }

    [HttpPost("admin/languages/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Language.Update)]
    public async Task<IActionResult> Edit(Guid id, UpdateLanguageVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Language updated.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update language.");
        return View(vm);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IActionResult> ReloadIndex(CreateLanguageVm create, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new LanguageListVm { Languages = list.Data.Languages, Create = create, ActiveOnly = false }
            : new LanguageListVm { Create = create };
        return View(nameof(Index), vm);
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("SignIn", "Auth", new { area = "Auth" });
}
