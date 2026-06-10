using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Search;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

/// <summary>
/// §2.2 Search — JSON gateway for the public search experience (suggest/businesses/
/// nearby/map) plus a permanent redirect from the retired /search page to /tours.
/// </summary>
[Area("Public")]
[AllowAnonymous]
public sealed class SearchController : BaseController
{
    private readonly SearchFacade _search;

    public SearchController(SearchFacade search) => _search = search;

    /// <summary>
    /// Phase 4.1: the standalone search page is retired — /tours is the canonical
    /// search surface (navbar overlay + filter offcanvas). Permanent redirect keeps
    /// old deep links and SEO equity alive. q/placeId/page are preserved; from/to/
    /// participants ride along as URL intent (backend availability filter is still
    /// a TODO — see SearchApiClient.SearchToursAsync notes).
    /// </summary>
    [HttpGet("search")]
    public IActionResult Index(
        string? q = null,
        Guid? placeId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int? participants = null,
        int page = 1)
    {
        return RedirectToActionPermanent("Index", "Tours", new
        {
            area = "Public",
            q,
            placeId,
            from,
            to,
            participants,
            page = page > 1 ? page : (int?)null
        });
    }

    /// <summary>
    /// AJAX suggest endpoint (S2: client debounces 300 ms). Returns a small JSON list.
    /// Output cache is intentionally NOT applied — suggest is request-specific and short-lived;
    /// the API itself is rate-limited (SEC5 30/min/IP).
    /// </summary>
    [HttpGet("search/suggest")]
    public async Task<IActionResult> Suggest(string q, CancellationToken ct = default)
    {
        var result = await _search.SuggestAsync(q, ct);
        var payload = (result.IsSuccess && result.Data is not null)
            ? result.Data
            : Array.Empty<TourSuggestResponse>();
        return Json(payload);
    }

    [HttpGet("search/businesses")]
    public async Task<IActionResult> Businesses(string? q = null, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _search.SearchBusinessesAsync(q, page, pageSize, ct);
        return Json(result.IsSuccess && result.Data is not null ? result.Data : new SearchBusinessRailVm { Query = q });
    }

    [HttpGet("search/nearby")]
    public async Task<IActionResult> Nearby(double lat, double lng, double radius = 10, int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _search.NearbyAsync(lat, lng, radius, pageSize, ct);
        return Json(result.IsSuccess && result.Data is not null ? result.Data : new SearchNearbyVm());
    }

    [HttpGet("search/map")]
    public async Task<IActionResult> Map(double northLat, double southLat, double eastLng, double westLng, CancellationToken ct = default)
    {
        var result = await _search.MapAsync(northLat, southLat, eastLng, westLng, ct);
        return Json(result.IsSuccess && result.Data is not null ? result.Data : new SearchMapVm());
    }
}
