using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Language.Read)]
public sealed class LanguagesController : BaseController
{
    private readonly LanguagesFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;
    public LanguagesController(LanguagesFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetLanguagesAsync(activeOnly, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
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
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Admin.Languages.Flash.Created"].Value);
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
        if (GuardSignOut(list) is { } signOut) return signOut;
        if (!list.IsSuccess || list.Data is null)
        {
            SetError(list.Error ?? _localizer["Admin.Languages.Flash.LoadFailed"].Value);
            return RedirectToAction(nameof(Index));
        }

        var row = list.Data.Languages.FirstOrDefault(l => l.Id == id);
        if (row is null)
        {
            SetError(_localizer["Admin.Languages.Flash.NotFound"].Value);
            return RedirectToAction(nameof(Index));
        }

        // PE1: deep links render the Index with the edit modal server-side open.
        return View(nameof(Index), new LanguageListVm
        {
            Languages = list.Data.Languages,
            EditId = id,
            Edit = new UpdateLanguageVm
            {
                Id         = row.Id,
                Name       = row.Name,
                NativeName = row.NativeName,
                IsRtl      = row.IsRtl,
                IsActive   = row.IsActive,
            },
        });
    }

    [HttpPost("admin/languages/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Language.Update)]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "Edit")] UpdateLanguageVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return await ReloadIndexForEdit(vm, ct);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Admin.Languages.Flash.Updated"].Value);
            return RedirectToAction(nameof(Index));
        }

        // The edit form lives in a modal on the Index view and binds under the
        // "Edit." prefix, so API field errors must be prefixed to match (UI-UX-F6).
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Edit.{field}", m);
            return await ReloadIndexForEdit(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update language.");
        return await ReloadIndexForEdit(vm, ct);
    }

    // F8 §4.7: re-render the Index with the edit modal open and field errors bound.
    private async Task<IActionResult> ReloadIndexForEdit(UpdateLanguageVm edit, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        var vm = new LanguageListVm
        {
            Languages = list.IsSuccess && list.Data is not null ? list.Data.Languages : [],
            EditId = edit.Id,
            Edit = edit,
        };
        return View(nameof(Index), vm);
    }


    private async Task<IActionResult> ReloadIndex(CreateLanguageVm create, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new LanguageListVm { Languages = list.Data.Languages, Create = create, ActiveOnly = false }
            : new LanguageListVm { Create = create };
        return View(nameof(Index), vm);
    }
}
