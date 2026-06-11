using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Specializations;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Specialization.Read)]
public sealed class SpecializationsController : BaseController
{
    private readonly SpecializationsFacade _facade;
    public SpecializationsController(SpecializationsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetAsync(activeOnly, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new SpecializationListVm { ActiveOnly = activeOnly });
        }
        return View(result.Data);
    }

    [HttpPost("admin/specializations/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Specialization.Create)]
    public async Task<IActionResult> Create(CreateSpecializationVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndex(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Specialization created.");
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Create.{field}", m);
            return await ReloadIndex(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create specialization.");
        return await ReloadIndex(vm, ct);
    }

    [HttpGet("admin/specializations/{id:guid}/edit")]
    [RequirePermission(WebPermission.Specialization.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var list = await _facade.GetAsync(activeOnly: false, ct);
        if (GuardSignOut(list) is { } signOut) return signOut;
        if (!list.IsSuccess || list.Data is null)
        {
            SetError(list.Error ?? "Could not load specializations.");
            return RedirectToAction(nameof(Index));
        }
        var row = list.Data.Specializations.FirstOrDefault(s => s.Id == id);
        if (row is null)
        {
            SetError("Specialization not found.");
            return RedirectToAction(nameof(Index));
        }
        // PE1: deep links render the Index with the edit modal server-side open.
        return View(nameof(Index), new SpecializationListVm
        {
            Specializations = list.Data.Specializations,
            EditId = id,
            Edit = new UpdateSpecializationVm
            {
                Id          = row.Id,
                Name        = row.Name,
                Description = row.Description,
                Icon        = row.Icon,
                IsActive    = row.IsActive,
            },
        });
    }

    [HttpPost("admin/specializations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Specialization.Update)]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "Edit")] UpdateSpecializationVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return await ReloadIndexForEdit(vm, ct);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Specialization updated.");
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            // UI-UX-F6: map API field errors to the Edit-prefixed modal inputs.
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Edit.{field}", m);
            return await ReloadIndexForEdit(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update specialization.");
        return await ReloadIndexForEdit(vm, ct);
    }

    private async Task<IActionResult> ReloadIndexForEdit(UpdateSpecializationVm edit, CancellationToken ct)
    {
        var list = await _facade.GetAsync(activeOnly: false, ct);
        var vm = new SpecializationListVm
        {
            Specializations = list.IsSuccess && list.Data is not null ? list.Data.Specializations : [],
            EditId = edit.Id,
            Edit = edit,
        };
        return View(nameof(Index), vm);
    }

    private async Task<IActionResult> ReloadIndex(CreateSpecializationVm create, CancellationToken ct)
    {
        var list = await _facade.GetAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new SpecializationListVm { Specializations = list.Data.Specializations, Create = create }
            : new SpecializationListVm { Create = create };
        return View(nameof(Index), vm);
    }
}
