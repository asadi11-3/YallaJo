using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Search;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

/// <summary>
/// API client for §2.2 Search. Wraps public search, map, and nearby endpoints.
/// </summary>
public sealed class SearchApiClient
{
    private const string ToursSearch = "/api/v1/tours/search";
    private const string ToursSuggest = "/api/v1/tours/search/suggest";
    private const string BusinessesSearch = "/api/v1/places/businesses/search";
    private const string BusinessesNearby = "/api/v1/places/businesses/nearby";
    private const string PlacesNearby = "/api/v1/places/nearby";
    private const string PlacesMapViewport = "/api/v1/places/map/viewport";

    private readonly IApiClient _api;

    public SearchApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// SSR result list. Backend caps <c>pageSize</c> at 50 (R4); caller is expected to clamp.
    /// <paramref name="placeId"/> filters results to tours operating at that place (backend supported,
    /// see <c>SearchToursRequest.PlaceId</c>). <c>from/to/participants</c> are NOT plumbed through —
    /// the backend has no availability-window filter yet, so the BFF only echoes them on the VM.
    /// </summary>
    public Task<ApiResult<PaginatedTourSearchResponse>> SearchToursAsync(
        string? query,
        int page,
        int pageSize,
        Guid? placeId = null,
        CancellationToken ct = default)
    {
        var qs = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };
        if (!string.IsNullOrWhiteSpace(query))
        {
            qs["q"] = query.Trim();
        }
        if (placeId is { } id && id != Guid.Empty)
        {
            qs["placeId"] = id.ToString();
        }
        var url = QueryHelpers.AddQueryString(ToursSearch, qs);
        return _api.GetAsync<PaginatedTourSearchResponse>(url, ct);
    }

    /// <summary>
    /// Autocomplete top-N suggestions (S2: debounce 300 ms client-side; SEC5: 30/min/IP).
    /// </summary>
    public Task<ApiResult<List<TourSuggestResponse>>> SuggestToursAsync(
        string query,
        CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(ToursSuggest, "q", query);
        return _api.GetAsync<List<TourSuggestResponse>>(url, ct);
    }

    public Task<ApiResult<PaginatedBusinessSearchResponse>> SearchBusinessesAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var qs = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(query))
        {
            qs["query"] = query.Trim();
        }

        var url = QueryHelpers.AddQueryString(BusinessesSearch, qs);
        return _api.GetAsync<PaginatedBusinessSearchResponse>(url, ct);
    }

    public Task<ApiResult<List<NearbyBusinessSearchResponse>>> GetNearbyBusinessesAsync(
        double lat,
        double lng,
        double radius,
        int pageSize,
        CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(BusinessesNearby, new Dictionary<string, string?>
        {
            ["lat"] = lat.ToString(CultureInfo.InvariantCulture),
            ["lng"] = lng.ToString(CultureInfo.InvariantCulture),
            ["radiusKm"] = radius.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });
        return _api.GetAsync<List<NearbyBusinessSearchResponse>>(url, ct);
    }

    public Task<ApiResult<List<NearbyPlaceSearchResponse>>> GetNearbyPlacesAsync(
        double lat,
        double lng,
        double radius,
        int pageSize,
        CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(PlacesNearby, new Dictionary<string, string?>
        {
            ["lat"] = lat.ToString(CultureInfo.InvariantCulture),
            ["lng"] = lng.ToString(CultureInfo.InvariantCulture),
            ["radiusKm"] = radius.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture)
        });
        return _api.GetAsync<List<NearbyPlaceSearchResponse>>(url, ct);
    }

    public Task<ApiResult<MapViewportResponse>> GetMapViewportAsync(
        double northLat,
        double southLat,
        double eastLng,
        double westLng,
        CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(PlacesMapViewport, new Dictionary<string, string?>
        {
            ["northLat"] = northLat.ToString(CultureInfo.InvariantCulture),
            ["southLat"] = southLat.ToString(CultureInfo.InvariantCulture),
            ["eastLng"] = eastLng.ToString(CultureInfo.InvariantCulture),
            ["westLng"] = westLng.ToString(CultureInfo.InvariantCulture)
        });
        return _api.GetAsync<MapViewportResponse>(url, ct);
    }
}
