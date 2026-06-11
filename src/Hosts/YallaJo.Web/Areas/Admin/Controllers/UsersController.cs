using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Users;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.User.Read)]
public sealed class UsersController : BaseController
{
    private readonly UsersFacade _facade;
    public UsersController(UsersFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _facade.GetUsersAsync(page, pageSize: 20, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return WantsAjax()
                ? PartialView("_UsersResults", new UserListVm())
                : View(new UserListVm());
        }

        return WantsAjax()
            ? PartialView("_UsersResults", result.Data)
            : View(result.Data);
    }

    [HttpGet("admin/users/details/{userId:guid}")]
    public async Task<IActionResult> Details(Guid userId, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(userId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/users/{userId:guid}/activate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.UpdateAny)]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        var result = await _facade.ActivateAsync(userId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "User activated.");
        return RedirectToAction("Details", new { userId });
    }

    [HttpPost("admin/users/{userId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.UpdateAny)]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(userId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "User deactivated.");
        return RedirectToAction("Details", new { userId });
    }

    [HttpPost("admin/users/{userId:guid}/roles")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.UserRole.Create)]
    public async Task<IActionResult> AssignRole(Guid userId, AssignRoleVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please select a role.");
            return RedirectToAction("Details", new { userId });
        }

        var result = await _facade.AssignRoleAsync(userId, vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        ApplyValidationErrors(result);
        SetFlash(result, "Role assigned.");
        return RedirectToAction("Details", new { userId });
    }

    [HttpPost("admin/users/{userId:guid}/roles/{roleId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.UserRole.Delete)]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var result = await _facade.RemoveRoleAsync(userId, roleId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Role removed.");
        return RedirectToAction("Details", new { userId });
    }

    [HttpPost("admin/users/{userId:guid}/claims")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.UpdateAny)]
    public async Task<IActionResult> AddClaim(Guid userId, AddClaimVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Claim type and value are required.");
            return RedirectToAction("Details", new { userId });
        }

        var result = await _facade.AddClaimAsync(userId, vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Claim added.");
        return RedirectToAction("Details", new { userId });
    }

    [HttpPost("admin/users/{userId:guid}/claims/{claimId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.User.UpdateAny)]
    public async Task<IActionResult> RemoveClaim(Guid userId, Guid claimId, CancellationToken ct)
    {
        var result = await _facade.RemoveClaimAsync(userId, claimId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Claim removed.");
        return RedirectToAction("Details", new { userId });
    }
}
