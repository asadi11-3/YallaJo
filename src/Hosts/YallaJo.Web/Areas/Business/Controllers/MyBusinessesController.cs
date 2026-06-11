using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.MyBusinesses;
using YallaJo.Web.Areas.Business.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class MyBusinessesController : BaseController
{
    private readonly MyBusinessesFacade _facade;

    public MyBusinessesController(MyBusinessesFacade facade) => _facade = facade;

    [HttpGet("business")]
    [HttpGet("business/businesses")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar("MyBusinesses", null);
        var result = await _facade.GetIndexAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new MyBusinessesVm());
        }
        return View(result.Data);
    }

    [HttpGet("business/businesses/register")]
    [RequirePermission(WebPermission.Business.Create)]
    public async Task<IActionResult> Register(CancellationToken ct = default)
    {
        SetSidebar("Register", null);
        var result = await _facade.GetRegisterAsync(ct: ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpPost("business/businesses/register")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Create)]
    public async Task<IActionResult> Register(RegisterBusinessFormVm form, CancellationToken ct = default)
    {
        SetSidebar("Register", null);
        if (!ModelState.IsValid)
            return await ReloadRegisterAsync(form, ct);

        var result = await _facade.RegisterAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadRegisterAsync(form, ct);
        }

        SetSuccess("Business registered. It is now pending review.");
        return RedirectToAction(nameof(Manage), new { id = result.Data });
    }

    [HttpGet("business/businesses/{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken ct = default)
    {
        SetSidebar("Manage", id);
        var result = await _facade.GetManageAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpPost("business/businesses/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Update)]
    public async Task<IActionResult> Update(Guid id, EditBusinessFormVm form, CancellationToken ct = default)
    {
        SetSidebar("Manage", id);
        form.Id = id;
        if (!ModelState.IsValid)
            return await ReloadManageAsync(id, form, ct);

        var result = await _facade.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result)) SetError(result.Error);
            return await ReloadManageAsync(id, form, ct);
        }

        SetSuccess("Business profile updated.");
        return RedirectToAction(nameof(Manage), new { id });
    }

    /// <summary>
    /// JSON proxy for the Register form's place typeahead (JS5: the browser never
    /// calls the API host directly). Backed by GET /api/v1/places/lookup.
    /// </summary>
    [HttpGet("business/businesses/places/lookup")]
    [RequirePermission(WebPermission.Business.Create)]
    public async Task<IActionResult> PlacesLookup(string? q, CancellationToken ct = default)
    {
        var term = q?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2)
            return Json(Array.Empty<object>());

        var result = await _facade.LookupPlacesAsync(term, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
            return Json(Array.Empty<object>());

        return Json(result.Data.Select(p => new { id = p.Id, name = p.Name, city = p.City }));
    }

    [HttpPost("business/businesses/{id:guid}/resubmit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Submit)]
    public async Task<IActionResult> Resubmit(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.ResubmitAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Business resubmitted for review.", "Could not resubmit the business for review.");
        return RedirectToAction(nameof(Manage), new { id });
    }

    private async Task<IActionResult> ReloadRegisterAsync(RegisterBusinessFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetRegisterAsync(form, ct);
        var vm = result is { IsSuccess: true, Data: not null }
            ? result.Data
            : new RegisterBusinessVm { Form = form, BusinessTypes = MyBusinessesMapper.BusinessTypeOptions(form.BusinessType) };
        return View(nameof(Register), vm);
    }

    private async Task<IActionResult> ReloadManageAsync(Guid id, EditBusinessFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetManageAsync(id, ct);
        var vm = result is { IsSuccess: true, Data: not null } ? result.Data : new ManageBusinessVm { Id = id };
        vm.Form = form;
        return View(nameof(Manage), vm);
    }

    private void SetSidebar(string nav, Guid? businessId)
    {
        ViewData["BusinessNav"] = nav;
        ViewBag.Sidebar = new BusinessSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Business",
            BusinessId = businessId,
        };
    }
}
