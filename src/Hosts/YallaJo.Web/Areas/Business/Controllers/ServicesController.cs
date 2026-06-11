using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Services;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class ServicesController : BusinessControllerBase
{
    private const string ListPartial = "_ServicesList";

    private readonly BusinessServicesFacade _facade;

    public ServicesController(BusinessServicesFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/services")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        SetSidebar("Services", id);
        var result = await _facade.GetAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "MyBusinesses");
        }
        return View(result.Data);
    }

    [HttpPost("business/businesses/{id:guid}/services/add")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ServiceItem.Create)]
    public async Task<IActionResult> Add(Guid id, AddServiceFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (WantsAjax())
            {
                return AjaxValidationProblem("Please provide a valid service name, price, and category.");
            }

            // No-JS validation failure: re-render the page with the submitted form so input is preserved (D-6, F1-F4).
            return await ReloadIndexAsync(id, form, ct);
        }

        var result = await _facade.AddAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (WantsAjax())
            {
                return AjaxFailure(result, "Could not add the service.");
            }

            ApplyValidationErrors(result);
            SetError(result.Error ?? "Could not add the service.");
            return await ReloadIndexAsync(id, form, ct);
        }

        if (WantsAjax())
        {
            return await ListPartialAsync(id, "Service added.", ct);
        }

        SetSuccess("Service added.");
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet("business/businesses/{id:guid}/services/{serviceId:guid}/edit")]
    [RequirePermission(WebPermission.ServiceItem.Update)]
    public async Task<IActionResult> Edit(Guid id, Guid serviceId, CancellationToken ct)
    {
        SetSidebar("Services", id);
        var result = await _facade.GetEditAsync(id, serviceId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index), new { id });
        }

        // AJAX: serve only the form partial for the edit offcanvas (PE1 enhancement).
        if (WantsAjax())
            return PartialView("_ServiceForm", result.Data);

        // No-JS: render the full Services page with the inline edit panel.
        return await IndexWithEditAsync(id, result.Data, ct);
    }

    [HttpPost("business/businesses/{id:guid}/services/{serviceId:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ServiceItem.Update)]
    public async Task<IActionResult> Edit(Guid id, Guid serviceId, EditServiceFormVm form, CancellationToken ct)
    {
        SetSidebar("Services", id);
        form.ServiceId = serviceId;
        form.BusinessId = id;

        if (!ModelState.IsValid)
        {
            if (WantsAjax())
            {
                return AjaxValidationProblem("Please provide a valid service name, price, and category.");
            }

            return await IndexWithEditAsync(id, form, ct);
        }

        var result = await _facade.UpdateAsync(id, serviceId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (WantsAjax())
            {
                return AjaxFailure(result, "Could not update the service.");
            }

            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return await IndexWithEditAsync(id, form, ct);
        }

        if (WantsAjax())
        {
            return await ListPartialAsync(id, "Service updated.", ct);
        }

        SetSuccess("Service updated.");
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("business/businesses/{id:guid}/services/{serviceId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ServiceItem.SoftDelete)]
    public async Task<IActionResult> Remove(Guid id, Guid serviceId, CancellationToken ct)
    {
        var result = await _facade.RemoveAsync(id, serviceId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return AjaxFailure(result, "Could not remove the service.");
            }

            return await ListPartialAsync(id, "Service removed.", ct);
        }

        SetFlash(result, "Service removed.", "Could not remove the service.");
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

    /// <summary>No-JS fallback: re-renders Index with the submitted add form preserved (D-6).</summary>
    private async Task<IActionResult> ReloadIndexAsync(Guid id, AddServiceFormVm form, CancellationToken ct)
    {
        SetSidebar("Services", id);
        var reload = await _facade.GetAsync(id, ct);
        var vm = reload.IsSuccess && reload.Data is not null
            ? reload.Data
            : new ServicesVm { BusinessId = id };
        vm.Form = form;
        return View(nameof(Index), vm);
    }

    /// <summary>
    /// Renders Services/Index with the inline edit panel populated — the no-JS
    /// surface that replaced the retired Services/Edit view.
    /// </summary>
    private async Task<IActionResult> IndexWithEditAsync(Guid id, EditServiceFormVm form, CancellationToken ct)
    {
        var listResult = await _facade.GetAsync(id, ct);
        var vm = listResult is { IsSuccess: true, Data: not null }
            ? listResult.Data
            : new ServicesVm { BusinessId = id, BusinessName = form.BusinessName };
        vm.EditForm = form;
        return View(nameof(Index), vm);
    }
}
