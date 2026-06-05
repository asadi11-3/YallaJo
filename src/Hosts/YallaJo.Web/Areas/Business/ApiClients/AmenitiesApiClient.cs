using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Business.Models.Amenities;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class AmenitiesApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public AmenitiesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<BusinessAmenityItemResponse>>> GetAmenitiesAsync(
        Guid businessId, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var url = QueryHelpers.AddQueryString($"{Base}/{businessId:D}/amenities", query);
        return _api.GetAsync<List<BusinessAmenityItemResponse>>(url, ct);
    }

    public Task<ApiResult> AddAsync(Guid businessId, AddBusinessAmenityApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/{businessId:D}/amenities", request, ct);

    public Task<ApiResult> RemoveAsync(Guid amenityId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{Base}/amenities/{amenityId:D}", ct);
}
