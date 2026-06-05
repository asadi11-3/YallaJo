using Microsoft.AspNetCore.WebUtilities;
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
}
