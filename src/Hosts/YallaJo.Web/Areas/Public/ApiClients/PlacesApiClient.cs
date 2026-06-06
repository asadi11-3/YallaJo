using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class PlacesApiClient
{
    private const string Base = "/api/v1/places";

    private readonly IApiClient _api;

    public PlacesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PlaceLookupResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PlaceLookupResponse>($"{Base}/{id}", ct);
    public Task<ApiResult<PaginatedPlacesResponse>> ListAsync(
        int page,
        int pageSize,
        string? city = null,
        string? country = null,
        int? ratingMin = null,
        bool hasActiveTours = false,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"]     = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        // Only attach filter params when set, so the API receives a clean query.
        if (!string.IsNullOrWhiteSpace(city))    query["city"]    = city.Trim();
        if (!string.IsNullOrWhiteSpace(country)) query["country"] = country.Trim();
        if (ratingMin is >= 1 and <= 5)          query["ratingMin"] = ratingMin.Value.ToString();
        if (hasActiveTours)                      query["hasActiveTours"] = "true";

        var url = QueryHelpers.AddQueryString(Base, query);
        return _api.GetAsync<PaginatedPlacesResponse>(url, ct);
    }

    public Task<ApiResult<PlaceDetailResponse>> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _api.GetAsync<PlaceDetailResponse>($"{Base}/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<List<PlaceImageResponse>>> GetImagesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<PlaceImageResponse>>($"{Base}/{id}/images", ct);

    public Task<ApiResult<List<PlaceAccessibilityFeatureResponse>>> GetAccessibilityAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<PlaceAccessibilityFeatureResponse>>($"{Base}/{id}/accessibility", ct);

    public Task<ApiResult<List<PlaceAccessibilityCatalogItemResponse>>> GetAccessibilityCatalogAsync(CancellationToken ct = default)
        => _api.GetAsync<List<PlaceAccessibilityCatalogItemResponse>>($"{Base}/accessibility/catalog", ct);

    public Task<ApiResult<PaginatedBusinessesResponse>> GetBusinessesAsync(Guid id, int page = 1, int pageSize = 6, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/{id}/businesses", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return _api.GetAsync<PaginatedBusinessesResponse>(url, ct);
    }

    public Task<ApiResult<RecommendationListResponse>> GetRecommendationsForAsync(
        string kind, Guid entityId, int limit = 4, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"/api/v1/analytics/recommendations/for/{Uri.EscapeDataString(kind)}/{entityId}",
            new Dictionary<string, string?> { ["limit"] = limit.ToString() });
        return _api.GetAsync<RecommendationListResponse>(url, ct);
    }

    public Task<ApiResult<RecommendationListResponse>> GetSimilarRecommendationsAsync(
        Guid entityId, string sourceKind = "Place", int limit = 4, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"/api/v1/analytics/recommendations/similar/{entityId}",
            new Dictionary<string, string?>
            {
                ["sourceKind"] = sourceKind,
                ["limit"] = limit.ToString(),
            });
        return _api.GetAsync<RecommendationListResponse>(url, ct);
    }

    public Task<ApiResult> RecordSponsoredClickAsync(SponsoredClickBody body, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/analytics/recommendations/sponsored-click", body, ct);
}
