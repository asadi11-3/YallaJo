using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Business.Models.Services;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class ServicesApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public ServicesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<ServiceItemResponse>>> GetServicesAsync(
        Guid businessId, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var url = QueryHelpers.AddQueryString($"{Base}/{businessId:D}/services", query);
        return _api.GetAsync<List<ServiceItemResponse>>(url, ct);
    }

    public Task<ApiResult> AddAsync(Guid businessId, CreateServiceItemApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/{businessId:D}/services", request, ct);

    public Task<ApiResult> RemoveAsync(Guid serviceId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{Base}/services/{serviceId:D}", ct);
}
