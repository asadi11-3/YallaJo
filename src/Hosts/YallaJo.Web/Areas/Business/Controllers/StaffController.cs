using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Staff;
using YallaJo.Web.Areas.Business.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class StaffController : BaseController
{
    private readonly BusinessStaffFacade _facade;

    public StaffController(BusinessStaffFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/staff")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct = default)
    {
        SetSidebar(id);
        var result = await _facade.GetAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "MyBusinesses");
        }

        return View(result.Data);
    }

    [HttpPost("business/businesses/{id:guid}/staff/add")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessStaff.Create)]
    public async Task<IActionResult> Add(Guid id, AddStaffFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please provide a valid user ID and role.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.AddAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Staff member added.", "Could not add the staff member.");
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("business/businesses/{id:guid}/staff/{staffId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessStaff.Delete)]
    public async Task<IActionResult> Remove(Guid id, Guid staffId, CancellationToken ct = default)
    {
        var result = await _facade.RemoveAsync(staffId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Staff member removed.", "Could not remove the staff member.");
        return RedirectToAction(nameof(Index), new { id });
    }

    private void SetSidebar(Guid businessId)
    {
        ViewData["BusinessNav"] = "Staff";
        ViewBag.Sidebar = new BusinessSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Business",
            BusinessId = businessId,
        };
    }
}
