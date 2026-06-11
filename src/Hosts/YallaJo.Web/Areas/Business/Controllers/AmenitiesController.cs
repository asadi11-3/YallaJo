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
            SetError("Please provide a valid amenity name.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.AddAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Amenity added.", "Could not add the amenity.");
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

        SetFlash(result, "Amenity removed.", "Could not remove the amenity.");
        return RedirectToAction(nameof(Index), new { id });
    }
}
