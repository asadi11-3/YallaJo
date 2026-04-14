using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users;

/// <summary>Admin user management — list, details, activate/deactivate, role + claim assignment.</summary>
[Area("Admin")]
[Authorize]
public sealed class UsersController : Controller
{
    private readonly UsersFacade _facade;
    public UsersController(UsersFacade facade) => _facade = facade;

    // GET /admin/users
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _facade.GetUsersAsync(page, pageSize: 20, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new UserListVm());
        }
        return View(result.Data);
    }

    // GET /admin/users/details/{userId}
    [HttpGet("admin/users/details/{userId:guid}")]
    public async Task<IActionResult> Details(Guid userId, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(userId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess) { TempData["Error"] = result.Error; return RedirectToAction(nameof(Index)); }
        return View(result.Data);
    }

    // POST /admin/users/{userId}/activate
    [HttpPost("admin/users/{userId:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        var result = await _facade.ActivateAsync(userId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "User activated." : result.Error;
        return RedirectToAction("Details", new { userId });
    }

    // POST /admin/users/{userId}/deactivate
    [HttpPost("admin/users/{userId:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        var result = await _facade.DeactivateAsync(userId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "User deactivated." : result.Error;
        return RedirectToAction("Details", new { userId });
    }

    // POST /admin/users/{userId}/roles
    [HttpPost("admin/users/{userId:guid}/roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(Guid userId, AssignRoleVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please select a role.";
            return RedirectToAction("Details", new { userId });
        }
        var result = await _facade.AssignRoleAsync(userId, vm, ct);
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
            TempData["Success"] = "Role assigned.";
        }
        return RedirectToAction("Details", new { userId });
    }

    // POST /admin/users/{userId}/roles/{roleId}/remove
    [HttpPost("admin/users/{userId:guid}/roles/{roleId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var result = await _facade.RemoveRoleAsync(userId, roleId, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Role removed." : result.Error;
        return RedirectToAction("Details", new { userId });
    }

    // POST /admin/users/{userId}/claims
    [HttpPost("admin/users/{userId:guid}/claims")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddClaim(Guid userId, AddClaimVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Claim type and value are required.";
            return RedirectToAction("Details", new { userId });
        }
        var result = await _facade.AddClaimAsync(userId, vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Claim added." : result.Error;
        return RedirectToAction("Details", new { userId });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Index", "Login", new { area = "Auth" });
}
