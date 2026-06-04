using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Listings;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class ListingsController : BaseController
{
    private readonly ListingsFacade _listings;

    public ListingsController(ListingsFacade listings) => _listings = listings;

    [HttpGet("provider/listings")]
    public async Task<IActionResult> Index(string? status = null, int page = 1, CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _listings.GetListingsAsync(status, page, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ListingsVm { StatusFilter = status });
        }

        return View(result.Data);
    }

    [HttpPost("provider/listings/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? status = null, int page = 1, CancellationToken ct = default)
    {
        var result = await _listings.DeleteAsync(id, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Listing deleted.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { status, page });
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Listings";
        ViewBag.Sidebar = new ProviderSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Provider",
        };
    }
}
