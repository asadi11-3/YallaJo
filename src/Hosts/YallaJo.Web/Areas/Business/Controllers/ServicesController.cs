using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Services;
using YallaJo.Web.Areas.Business.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class ServicesController : BaseController
{
    private readonly BusinessServicesFacade _facade;

    public ServicesController(BusinessServicesFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/services")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        SetSidebar(id);
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

    [HttpPost("business/businesses/{id:guid}/services/{serviceId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ServiceItem.SoftDelete)]
    public async Task<IActionResult> Remove(Guid id, Guid serviceId, CancellationToken ct)
    {
        var result = await _facade.RemoveAsync(serviceId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Service removed.", "Could not remove the service.");
        return RedirectToAction(nameof(Index), new { id });
    }

    private void SetSidebar(Guid businessId)
    {
        ViewData["BusinessNav"] = "Services";
        ViewBag.Sidebar = new BusinessSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Business",
            BusinessId = businessId,
        };
    }
}
