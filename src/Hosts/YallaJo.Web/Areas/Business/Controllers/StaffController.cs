using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Staff;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class StaffController : BusinessControllerBase
{
    private const string ListPartial = "_StaffList";

    private readonly BusinessStaffFacade _facade;

    public StaffController(BusinessStaffFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/staff")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct = default)
    {
        SetSidebar("Staff", id);
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

    /// <summary>
    /// JSON proxy for the staff picker typeahead (F10/JS5 — the browser never calls the API host).
    /// Same permission as adding staff; debounced 300ms client-side (J4).
    /// </summary>
    [HttpGet("business/businesses/{id:guid}/staff/lookup")]
    [RequirePermission(WebPermission.BusinessStaff.Create)]
    public async Task<IActionResult> Lookup(Guid id, string? q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var result = await _facade.LookupAsync(q.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return Json(Array.Empty<object>());
        }

        return Json(result.Data.Select(u => new
        {
            id = u.Id,
            displayName = u.DisplayName,
            email = u.Email,
        }));
    }

    [HttpPost("business/businesses/{id:guid}/staff/add")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessStaff.Create)]
    public async Task<IActionResult> Add(Guid id, AddStaffFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            if (WantsAjax())
            {
                return AjaxValidationProblem("Please provide a valid user ID and role.");
            }

            // No-JS validation failure: re-render the page with the submitted form so input is preserved (D-6, F1-F4).
            return await ReloadIndexAsync(id, form, ct);
        }

        var result = await _facade.AddAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (WantsAjax())
            {
                return AjaxFailure(result, "Could not add the staff member.");
            }

            ApplyValidationErrors(result);
            SetError(result.Error ?? "Could not add the staff member.");
            return await ReloadIndexAsync(id, form, ct);
        }

        if (WantsAjax())
        {
            return await ListPartialAsync(id, "Staff member added.", ct);
        }

        SetSuccess("Staff member added.");
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("business/businesses/{id:guid}/staff/{staffId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessStaff.Delete)]
    public async Task<IActionResult> Remove(Guid id, Guid staffId, CancellationToken ct = default)
    {
        var result = await _facade.RemoveAsync(id, staffId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return AjaxFailure(result, "Could not remove the staff member.");
            }

            return await ListPartialAsync(id, "Staff member removed.", ct);
        }

        SetFlash(result, "Staff member removed.", "Could not remove the staff member.");
        return RedirectToAction(nameof(Index), new { id });
    }

    /// <summary>Returns the refreshed list partial after a successful AJAX mutation; falls back to PRG when reload fails.</summary>
    private async Task<IActionResult> ListPartialAsync(Guid id, string toast, CancellationToken ct)
    {
        var reload = await _facade.GetAsync(id, ct);
        if (!reload.IsSuccess || reload.Data is null)
        {
            SetSuccess(toast);
            return RedirectToAction(nameof(Index), new { id });
        }

        SetAjaxToast(toast);
        return PartialView(ListPartial, reload.Data);
    }

    /// <summary>No-JS fallback: re-renders Index with the submitted form preserved (D-6).</summary>
    private async Task<IActionResult> ReloadIndexAsync(Guid id, AddStaffFormVm form, CancellationToken ct)
    {
        SetSidebar("Staff", id);
        var reload = await _facade.GetAsync(id, ct);
        var vm = reload.IsSuccess && reload.Data is not null
            ? reload.Data
            : new StaffVm { BusinessId = id };
        vm.Form = form;
        return View(nameof(Index), vm);
    }
}
