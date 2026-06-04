using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Businesses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class BusinessesApiClient
{
    private readonly IApiClient _api;

    public BusinessesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<BusinessPageResponse>> GetByPlaceAsync(
        Guid placeId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(
            $"/api/v1/places/{placeId:D}/businesses",
            new Dictionary<string, string?>
            {
                ["page"] = page.ToString(CultureInfo.InvariantCulture),
                ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
            });
        return _api.GetAsync<BusinessPageResponse>(url, ct);
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/places/businesses/admin/{id:D}/approve", null, ct);

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/places/businesses/admin/{id:D}/reject", new { reason }, ct);

    public Task<ApiResult> RequestMoreDocsAsync(Guid id, string reason, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/places/businesses/admin/{id:D}/request-more-docs", new { reason }, ct);

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/places/businesses/admin/{id:D}/suspend", new { reason }, ct);

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/places/businesses/admin/{id:D}/reinstate", null, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"/api/v1/places/businesses/{id:D}", ct);
}
