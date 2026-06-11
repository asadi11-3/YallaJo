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
            SetError("Please provide a valid service name, price, and category.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.AddAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Service added.", "Could not add the service.");
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
            return await IndexWithEditAsync(id, form, ct);

        var result = await _facade.UpdateAsync(id, serviceId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return await IndexWithEditAsync(id, form, ct);
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
        SetFlash(result, "Service removed.", "Could not remove the service.");
        return RedirectToAction(nameof(Index), new { id });
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
