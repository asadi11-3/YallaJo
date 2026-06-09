using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Search;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

/// <summary>
/// §2.2 Search. SSR results grid at <c>/search</c> + JSON autocomplete at <c>/search/suggest</c>.
/// </summary>
[Area("Public")]
[AllowAnonymous]
public sealed class SearchController : BaseController
{
    private readonly SearchFacade _search;

    public SearchController(SearchFacade search) => _search = search;

    [HttpGet("search")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<IActionResult> Index(
        string? q = null,
        Guid? placeId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int? participants = null,
        int page = 1,
        int pageSize = SearchFacade.DefaultPageSize,
        CancellationToken ct = default)
    {
        var result = await _search.SearchAsync(q, placeId, from, to, participants, page, pageSize, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new SearchVm
            {
                Query = q,
                PlaceId = placeId,
                From = from,
                To = to,
                Participants = participants,
                PageNumber = page,
                PageSize = pageSize
            });
        }
        return View(result.Data);
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
