using System.Globalization;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places;

public sealed class PlacesApiClient
{
    private const string BasePath = "/api/v1/places";

    private readonly IApiClient _api;
    public PlacesApiClient(IApiClient api) => _api = api;

    // ── Queries ──────────────────────────────────────────────────────────────
    public Task<ApiResult<PaginatedPlacesResponse>> ListAsync(
        int page,
        int pageSize,
        PlaceListFilterVm filter,
        CancellationToken ct = default)
    {
        // BuildListQuery already returns a leading "?". Concatenating with BasePath
        // directly avoids the cosmetic "/?" form ("/api/v1/places/?page=1") that an
        // intermediate slash would produce.
        var query = BuildListQuery(page, pageSize, filter);
        return _api.GetAsync<PaginatedPlacesResponse>($"{BasePath}{query}", ct);
    }

    public Task<ApiResult<PlaceDetailsResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PlaceDetailsResponse>($"{BasePath}/{id}", ct);

    // ── Commands ─────────────────────────────────────────────────────────────
    public Task<ApiResult<CreatePlaceResponse>> CreateAsync(
        CreatePlaceRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreatePlaceResponse>(BasePath, request, ct);

    public Task<ApiResult> UpdateAsync(
        Guid id, UpdatePlaceRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{BasePath}/{id}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{BasePath}/{id}", ct);

    public Task<ApiResult> FeatureAsync(Guid id, bool featured, CancellationToken ct = default)
        => _api.PatchAsync(
            $"{BasePath}/{id}/feature?featured={(featured ? "true" : "false")}",
            body: null,
            ct);

    public Task<ApiResult> VerifyAsync(Guid id, bool verified, CancellationToken ct = default)
        => _api.PatchAsync(
            $"{BasePath}/{id}/verify?verified={(verified ? "true" : "false")}",
            body: null,
            ct);

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string BuildListQuery(int page, int pageSize, PlaceListFilterVm filter)
    {
        var parts = new List<string>(8)
        {
            $"page={page}",
            $"pageSize={pageSize}",
        };

        if (filter.CategoryId is { } catId && catId != Guid.Empty)
            parts.Add($"categoryId={catId}");

        if (filter.RatingMin is { } min)
            parts.Add($"ratingMin={min.ToString(CultureInfo.InvariantCulture)}");

        if (filter.RatingMax is { } max)
            parts.Add($"ratingMax={max.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(filter.City))
            parts.Add($"city={Uri.EscapeDataString(filter.City.Trim())}");

        if (!string.IsNullOrWhiteSpace(filter.Country))
            parts.Add($"country={Uri.EscapeDataString(filter.Country.Trim())}");

        if (filter.HasActiveTours is { } hat)
            parts.Add($"hasActiveTours={(hat ? "true" : "false")}");

        return "?" + string.Join("&", parts);
    }
}
