using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;

/// <summary>Admin role management — list, create, update description, deactivate, claims.</summary>
[Area("Admin")]
[Authorize]
public sealed class RolesController : Controller
{
    private readonly RolesFacade _facade;
    public RolesController(RolesFacade facade) => _facade = facade;

    // GET /admin/roles
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetRolesAsync(ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess) { ViewBag.Error = result.Error; return View(new RoleListVm()); }
        return View(result.Data);
    }

    // POST /admin/roles/create
    [HttpPost("admin/roles/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Re-show the list with the form errors
            var listResult = await _facade.GetRolesAsync(ct);
            var listVm = listResult.IsSuccess ? listResult.Data! : new RoleListVm();
            listVm = new RoleListVm { Roles = listVm.Roles, Create = vm };
            return View("Index", listVm);
        }

        var result = await _facade.CreateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors is not null)
                foreach (var (f, msgs) in result.ValidationErrors)
                    foreach (var m in msgs) ModelState.AddModelError(f, m);
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Success"] = $"Role '{vm.Name}' created.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST /admin/roles/{roleId}/update
    [HttpPost("admin/roles/{roleId:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid roleId, UpdateRoleVm vm, CancellationToken ct)
    {
        var result = await _facade.UpdateAsync(roleId, vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Role updated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // POST /admin/roles/{roleId}/deactivate
    [HttpPost("admin/roles/{roleId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid roleId, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(roleId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Role deactivated." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // POST /admin/roles/{roleId}/claims
    [HttpPost("admin/roles/{roleId:guid}/claims")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClaim(Guid roleId, AddRoleClaimVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Claim type and value are required."; return RedirectToAction(nameof(Index)); }
        var result = await _facade.AddClaimAsync(roleId, vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Claim added." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // POST /admin/roles/{roleId}/claims/{claimId}/remove
    [HttpPost("admin/roles/{roleId:guid}/claims/{claimId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveClaim(Guid roleId, Guid claimId, CancellationToken ct)
    {
        var result = await _facade.RemoveClaimAsync(roleId, claimId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Claim removed." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Index", "Login", new { area = "Auth" });
}
