using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Home;
using YallaJo.Web.Areas.Public.Models.Search;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>
/// Facade for §2.2 Search. Returns an always-Ok <see cref="ApiResult{T}"/> with an empty
/// echoed view-model on failure (best-effort SSR — keeps the page renderable on partial outage,
/// matches the Home/Tours house style).
/// </summary>
public sealed class SearchFacade
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    private readonly SearchApiClient _api;

    public SearchFacade(SearchApiClient api) => _api = api;

    /// <summary>
    /// Primary SSR entry point for <c>GET /search</c>.
    /// </summary>
    public async Task<ApiResult<SearchVm>> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var clampedPage = page < 1 ? 1 : page;
        var clampedSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
        var trimmed = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        var empty = new SearchVm
        {
            Query = trimmed,
            PageNumber = clampedPage,
            PageSize = clampedSize
        };

        try
        {
            var result = await _api.SearchToursAsync(trimmed, clampedPage, clampedSize, ct);
            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<SearchVm>.Ok(empty);
            }

            var data = result.Data;
            var items = data.Items
                .Select(MapItem)
                .ToList();

            var vm = new SearchVm
            {
                Query = trimmed,
                Items = items,
                PageNumber = data.PageNumber,
                PageSize = data.PageSize,
                TotalCount = data.TotalCount,
                HasPreviousPage = data.HasPreviousPage,
                HasNextPage = data.HasNextPage
            };
            return ApiResult<SearchVm>.Ok(vm);
        }
        catch
        {
            // Search must never throw out of the SSR controller — empty result page is the fallback.
            return ApiResult<SearchVm>.Ok(empty);
        }
    }

    /// <summary>
    /// Autocomplete entry point for <c>GET /search/suggest</c>. Returns at most ~5 suggestions
    /// (backend already caps; we just shape the response).
    /// </summary>
    public async Task<ApiResult<IReadOnlyList<TourSuggestResponse>>> SuggestAsync(
        string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return ApiResult<IReadOnlyList<TourSuggestResponse>>.Ok(Array.Empty<TourSuggestResponse>());
        }

        try
        {
            var result = await _api.SuggestToursAsync(query.Trim(), ct);
            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<IReadOnlyList<TourSuggestResponse>>.Ok(Array.Empty<TourSuggestResponse>());
            }
            return ApiResult<IReadOnlyList<TourSuggestResponse>>.Ok(result.Data);
        }
        catch
        {
            return ApiResult<IReadOnlyList<TourSuggestResponse>>.Ok(Array.Empty<TourSuggestResponse>());
        }
    }

    public async Task<ApiResult<SearchBusinessRailVm>> SearchBusinessesAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var clampedPage = page < 1 ? 1 : page;
        var clampedSize = Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, MaxPageSize);
        var trimmed = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var empty = new SearchBusinessRailVm { Query = trimmed, PageNumber = clampedPage, PageSize = clampedSize };

        try
        {
            var result = await _api.SearchBusinessesAsync(trimmed, clampedPage, clampedSize, ct);
            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<SearchBusinessRailVm>.Ok(empty);
            }

            var data = result.Data;
            return ApiResult<SearchBusinessRailVm>.Ok(new SearchBusinessRailVm
            {
                Query = trimmed,
                Items = data.Items.Select(MapBusiness).ToList(),
                PageNumber = data.PageNumber,
                PageSize = data.PageSize,
                TotalCount = data.TotalCount
            });
        }
        catch
        {
            return ApiResult<SearchBusinessRailVm>.Ok(empty);
        }
    }

    public async Task<ApiResult<SearchNearbyVm>> NearbyAsync(
        double lat,
        double lng,
        double radius,
        int pageSize,
        CancellationToken ct = default)
    {
        var clampedRadius = Math.Clamp(radius <= 0 ? 10 : radius, 1, 100);
        var clampedSize = Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, MaxPageSize);

        try
        {
            var placesTask = _api.GetNearbyPlacesAsync(lat, lng, clampedRadius, clampedSize, ct);
            var businessesTask = _api.GetNearbyBusinessesAsync(lat, lng, clampedRadius, clampedSize, ct);
            await Task.WhenAll(placesTask, businessesTask);

            var places = placesTask.Result is { IsSuccess: true, Data: { } placeData }
                ? placeData.Select(MapNearbyPlace).ToList()
                : [];
            var businesses = businessesTask.Result is { IsSuccess: true, Data: { } businessData }
                ? businessData.Select(MapNearbyBusiness).ToList()
                : [];

            return ApiResult<SearchNearbyVm>.Ok(new SearchNearbyVm { Places = places, Businesses = businesses });
        }
        catch
        {
            return ApiResult<SearchNearbyVm>.Ok(new SearchNearbyVm());
        }
    }

    public async Task<ApiResult<SearchMapVm>> MapAsync(
        double northLat,
        double southLat,
        double eastLng,
        double westLng,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetMapViewportAsync(northLat, southLat, eastLng, westLng, ct);
            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<SearchMapVm>.Ok(new SearchMapVm());
            }

            return ApiResult<SearchMapVm>.Ok(new SearchMapVm
            {
                Pins = result.Data.Pins.Select(MapPin).ToList(),
                IsClusteringRecommended = result.Data.IsClusteringRecommended
            });
        }
        catch
        {
            return ApiResult<SearchMapVm>.Ok(new SearchMapVm());
        }
    }

    private static HomeTourCardVm MapItem(TourSearchItemResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        ImageUrl = PublicImagePlaceholder.ResolveTourImage(item.Id),
        BasePrice = item.BasePrice,
        SalePrice = item.SalePrice,
        Currency = string.IsNullOrWhiteSpace(item.Currency) ? "USD" : item.Currency,
        AverageRating = item.AverageRating,
        ReviewCount = item.ReviewCount,
        BookingCount = item.BookingCount,
        IsFeatured = item.IsFeatured
    };

    private static SearchBusinessVm MapBusiness(BusinessSearchItemResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        BusinessType = item.BusinessType,
        Lat = item.Lat,
        Lng = item.Lng,
        City = item.City,
        Country = item.Country,
        AverageRating = item.AverageRating,
        ReviewCount = item.ReviewCount,
        IsVerified = item.IsVerified,
        IsFeatured = item.IsFeatured,
        PrimaryImageUrl = item.PrimaryImageUrl
    };

    private static SearchNearbyPlaceVm MapNearbyPlace(NearbyPlaceSearchResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        Latitude = item.Latitude,
        Longitude = item.Longitude,
        AverageRating = item.AverageRating,
        DistanceKm = item.DistanceKm
    };

    private static SearchNearbyBusinessVm MapNearbyBusiness(NearbyBusinessSearchResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        BusinessType = item.BusinessType,
        Latitude = item.Latitude,
        Longitude = item.Longitude,
        City = item.City,
        Country = item.Country,
        AverageRating = item.AverageRating,
        ReviewCount = item.ReviewCount,
        IsVerified = item.IsVerified,
        IsFeatured = item.IsFeatured,
        DistanceKm = item.DistanceKm
    };

    private static SearchMapPinVm MapPin(MapPinResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Lat = item.Lat,
        Lng = item.Lng,
        PrimaryImageUrl = item.PrimaryImageUrl,
        AverageRating = item.AverageRating,
        TourCount = item.TourCount
    };
}
