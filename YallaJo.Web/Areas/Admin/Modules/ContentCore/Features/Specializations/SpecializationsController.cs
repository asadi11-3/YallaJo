using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Specialization.Read)]
public sealed class SpecializationsController : Controller
{
    private readonly SpecializationsFacade _facade;
    public SpecializationsController(SpecializationsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetAsync(activeOnly, ct);
        if (result.RequireSignOut) return RedirectToLogin();
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
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Specialization created.";
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
        if (list.RequireSignOut) return RedirectToLogin();
        if (!list.IsSuccess || list.Data is null)
        {
            TempData["Error"] = list.Error ?? "Could not load specializations.";
            return RedirectToAction(nameof(Index));
        }
        var row = list.Data.Specializations.FirstOrDefault(s => s.Id == id);
        if (row is null)
        {
            TempData["Error"] = "Specialization not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(new UpdateSpecializationVm
        {
            Id          = row.Id,
            Name        = row.Name,
            Description = row.Description,
            Icon        = row.Icon,
            IsActive    = row.IsActive,
        });
    }

    [HttpPost("admin/specializations/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Specialization.Update)]
    public async Task<IActionResult> Edit(Guid id, UpdateSpecializationVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Specialization updated.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update specialization.");
        return View(vm);
    }

    private async Task<IActionResult> ReloadIndex(CreateSpecializationVm create, CancellationToken ct)
    {
        var list = await _facade.GetAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new SpecializationListVm { Specializations = list.Data.Specializations, Create = create }
            : new SpecializationListVm { Create = create };
        return View(nameof(Index), vm);
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("Index", "Login", new { area = "Auth" });
}
