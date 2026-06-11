using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Business.Models.MyBusinesses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class MyBusinessesApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public MyBusinessesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<BusinessSummaryResponse>>> GetMineAsync(
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var url = QueryHelpers.AddQueryString($"{Base}/mine", query);
        return _api.GetAsync<List<BusinessSummaryResponse>>(url, ct);
    }

    public Task<ApiResult<BusinessDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<BusinessDetailResponse>($"{Base}/{id:D}", ct);

    /// <summary>
    /// Creates a new business (POST /api/v1/places/businesses). Returns the created
    /// business detail so the caller can redirect straight into its management pages.
    /// </summary>
    public Task<ApiResult<BusinessDetailResponse>> CreateAsync(CreateBusinessApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<BusinessDetailResponse>(Base, request, ct);

    /// <summary>
    /// Loads places to populate the Register form's Place picker. Uses the public
    /// places list (GET /api/v1/places); page size is generous so the dropdown can
    /// show the full set without paging.
    /// </summary>
    public Task<ApiResult<PlaceOptionsResponse>> GetPlaceOptionsAsync(int pageSize = 200, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = "1",
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var url = QueryHelpers.AddQueryString("/api/v1/places", query);
        return _api.GetAsync<PlaceOptionsResponse>(url, ct);
    }

    /// <summary>
    /// Typeahead place search (GET /api/v1/places/lookup). Powers the Register
    /// form's searchable place picker; the full options list remains the no-JS path.
    /// </summary>
    public Task<ApiResult<List<PlaceLookupItemResponse>>> LookupPlacesAsync(
        string term, int pageSize = 10, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["term"] = term,
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var url = QueryHelpers.AddQueryString("/api/v1/places/lookup", query);
        return _api.GetAsync<List<PlaceLookupItemResponse>>(url, ct);
    }

    public Task<ApiResult> UpdateAsync(Guid id, UpdateBusinessApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id:D}", request, ct);

    public Task<ApiResult> ResubmitAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id:D}/resubmit", null, ct);
}
