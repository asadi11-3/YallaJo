using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;
using YallaJo.Web.Areas.Business.Models.Amenities;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Business.Controllers;

[Area("Business")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class AmenitiesController : BusinessControllerBase
{
    private const string ListPartial = "_AmenitiesList";

    private readonly BusinessAmenitiesFacade _facade;

    public AmenitiesController(BusinessAmenitiesFacade facade) => _facade = facade;

    [HttpGet("business/businesses/{id:guid}/amenities")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        SetSidebar("Amenities", id);
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

    [HttpPost("business/businesses/{id:guid}/amenities/add")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessAmenity.Create)]
    public async Task<IActionResult> Add(Guid id, AddAmenityFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (WantsAjax())
            {
                return AjaxValidationProblem(L["Business.Flash.AmenityInvalid"].Value);
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
                return AjaxFailure(result, L["Business.Error.AddAmenityFailed"].Value);
            }

            ApplyValidationErrors(result);
            SetError(result.Error ?? L["Business.Error.AddAmenityFailed"].Value);
            return await ReloadIndexAsync(id, form, ct);
        }

        if (WantsAjax())
        {
            return await ListPartialAsync(id, L["Business.Flash.AmenityAdded"].Value, ct);
        }

        SetSuccess(L["Business.Flash.AmenityAdded"].Value);
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("business/businesses/{id:guid}/amenities/{amenityId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BusinessAmenity.Delete)]
    public async Task<IActionResult> Remove(Guid id, Guid amenityId, CancellationToken ct)
    {
        var result = await _facade.RemoveAsync(id, amenityId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return AjaxFailure(result, L["Business.Error.RemoveAmenityFailed"].Value);
            }

            return await ListPartialAsync(id, L["Business.Flash.AmenityRemoved"].Value, ct);
        }

        SetFlash(result, L["Business.Flash.AmenityRemoved"].Value, L["Business.Error.RemoveAmenityFailed"].Value);
        return RedirectToAction(nameof(Index), new { id });
    }

    /// <summary>Returns the refreshed list partial after a successful AJAX mutation; falls back to PRG when reload fails.</summary>
    private async Task<IActionResult> ListPartialAsync(Guid id, string toast, CancellationToken ct)
    {
        var reload = await _facade.GetAsync(id, ct);
        if (!reload.IsSuccess || reload.Data is null)
        {
            // Mutation succeeded but reload failed - fall back to PRG so the user still sees fresh data.
            SetSuccess(toast);
            return RedirectToAction(nameof(Index), new { id });
        }

        SetAjaxToast(toast);
        return PartialView(ListPartial, reload.Data);
    }

    /// <summary>No-JS fallback: re-renders Index with the submitted form preserved (D-6).</summary>
    private async Task<IActionResult> ReloadIndexAsync(Guid id, AddAmenityFormVm form, CancellationToken ct)
    {
        SetSidebar("Amenities", id);
        var reload = await _facade.GetAsync(id, ct);
        var vm = reload.IsSuccess && reload.Data is not null
            ? reload.Data
            : new AmenitiesVm { BusinessId = id };
        vm.Form = form;
        return View(nameof(Index), vm);
    }
}
