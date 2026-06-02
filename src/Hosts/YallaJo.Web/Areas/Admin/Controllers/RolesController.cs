using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Roles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authorization;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Role.Read)]
public sealed class RolesController : Controller
{
    private readonly RolesFacade _facade;
    public RolesController(RolesFacade facade) => _facade = facade;


    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetRolesAsync(ct);

        return result.State switch
        {
            ApiResultState.Success      => View(result.Data),
            ApiResultState.Unauthorized => RedirectToLogin(),
            ApiResultState.Forbidden    => new ForbidResult(),
            _ => ViewWithError(new RoleListVm(), result.Error),
        };
    }

    [HttpGet("admin/roles/details/{roleId:guid}")]
    public async Task<IActionResult> Details(Guid roleId, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(roleId, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/roles/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Create)]
    public async Task<IActionResult> Create(CreateRoleVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var listResult = await _facade.GetRolesAsync(ct);
            if (listResult.State == ApiResultState.Unauthorized) return RedirectToLogin();
            if (listResult.State == ApiResultState.Forbidden)    return new ForbidResult();

            return View("Index", new RoleListVm
            {
                Roles  = listResult.Data?.Roles ?? [],
                Create = vm,
            });
        }

        var result = await _facade.CreateAsync(vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            foreach (var (field, msgs) in result.ValidationErrors)
                foreach (var msg in msgs) ModelState.AddModelError(field, msg);
        }

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Role '{vm.Name}' created." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/roles/{roleId:guid}/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Update)]
    public async Task<IActionResult> Update(Guid roleId, UpdateRoleVm vm, CancellationToken ct)
    {
        var result = await _facade.UpdateAsync(roleId, vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            foreach (var (field, msgs) in result.ValidationErrors)
                foreach (var msg in msgs) ModelState.AddModelError(field, msg);
        }

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Role updated." : result.Error;

        return RedirectToAction(nameof(Details), new { roleId });
    }

    [HttpPost("admin/roles/{roleId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Update)]
    public async Task<IActionResult> Deactivate(Guid roleId, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(roleId, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Role deactivated." : result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/roles/{roleId:guid}/claims")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.RoleClaim.Create)]
    public async Task<IActionResult> AddClaim(Guid roleId, AddRoleClaimVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Claim type and value are required.";
            return RedirectToAction(nameof(Details), new { roleId });
        }

        var result = await _facade.AddClaimAsync(roleId, vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Claim added." : result.Error;

        return RedirectToAction(nameof(Details), new { roleId });
    }

    [HttpPost("admin/roles/{roleId:guid}/claims/{claimId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.RoleClaim.Delete)]
    public async Task<IActionResult> RemoveClaim(Guid roleId, Guid claimId, CancellationToken ct)
    {
        var result = await _facade.RemoveClaimAsync(roleId, claimId, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Claim removed." : result.Error;

        return RedirectToAction(nameof(Details), new { roleId });
    }

    private RedirectToActionResult RedirectToLogin() =>
        RedirectToAction("SignIn", "Auth", new { area = "Auth" });

    private ViewResult ViewWithError(RoleListVm vm, string? error)
    {
        ViewBag.Error = error;
        return View(vm);
    }
}
