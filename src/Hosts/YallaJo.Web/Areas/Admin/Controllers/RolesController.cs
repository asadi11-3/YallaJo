using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Roles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Role.Read)]
public sealed class RolesController : BaseController
{
    private readonly RolesFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;
    public RolesController(RolesFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }


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
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/roles/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Create)]
    public async Task<IActionResult> Create(CreateRoleVm vm, CancellationToken ct)
    {
        // Client-side model invalid: re-render the create form on the Index view.
        if (!ModelState.IsValid)
            return await RenderIndexWithCreate(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Admin.Roles.Flash.Created", vm.Name].Value);
            return RedirectToAction(nameof(Index));
        }

        // Failure: re-render the Index view with the create form so field-level errors
        // are preserved (UI-UX-F6). A redirect here would discard ModelState.
        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create role.");

        return await RenderIndexWithCreate(vm, ct);
    }

    private async Task<IActionResult> RenderIndexWithCreate(CreateRoleVm vm, CancellationToken ct)
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

    [HttpPost("admin/roles/{roleId:guid}/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Update)]
    public async Task<IActionResult> Update(Guid roleId, UpdateRoleVm vm, CancellationToken ct)
    {
        var result = await _facade.UpdateAsync(roleId, vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Admin.Roles.Flash.Updated"].Value);
            return RedirectToAction(nameof(Details), new { roleId });
        }

        // Failure: re-render the Details view with field-level errors (UI-UX-F6) rather
        // than redirecting (which would discard ModelState). Reload the detail model first.
        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Admin.Roles.Flash.UpdateFailed"].Value);

        var details = await _facade.GetDetailsAsync(roleId, ct);
        if (details.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (details.State == ApiResultState.Forbidden)    return new ForbidResult();
        if (!details.IsSuccess || details.Data is null)
        {
            // Can't re-render without the model; fall back to a flash on Details.
            SetError(result.Error ?? _localizer["Admin.Roles.Flash.UpdateFailed"].Value);
            return RedirectToAction(nameof(Details), new { roleId });
        }

        return View(nameof(Details), details.Data);
    }

    [HttpPost("admin/roles/{roleId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Role.Update)]
    public async Task<IActionResult> Deactivate(Guid roleId, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(roleId, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        SetFlash(result, _localizer["Admin.Roles.Flash.Deactivated"].Value);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/roles/{roleId:guid}/claims")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.RoleClaim.Create)]
    public async Task<IActionResult> AddClaim(Guid roleId, AddRoleClaimVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Add-claim is a modal on the Details page with no view of its own; surface
            // the guard as a flash on the redirect target.
            SetError(_localizer["Admin.Shared.Flash.ClaimTypeValueRequired"].Value);
            return RedirectToAction(nameof(Details), new { roleId });
        }

        var result = await _facade.AddClaimAsync(roleId, vm, ct);

        if (result.State == ApiResultState.Unauthorized) return RedirectToLogin();
        if (result.State == ApiResultState.Forbidden)    return new ForbidResult();

        SetFlash(result, _localizer["Admin.Shared.Flash.ClaimAdded"].Value);

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

        SetFlash(result, _localizer["Admin.Shared.Flash.ClaimRemoved"].Value);

        return RedirectToAction(nameof(Details), new { roleId });
    }

    private ViewResult ViewWithError(RoleListVm vm, string? error)
    {
        ViewBag.Error = error;
        return View(vm);
    }
}
