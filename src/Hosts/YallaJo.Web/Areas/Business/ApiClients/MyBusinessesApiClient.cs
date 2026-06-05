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

    public Task<ApiResult> UpdateAsync(Guid id, UpdateBusinessApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id:D}", request, ct);

    public Task<ApiResult> ResubmitAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id:D}/resubmit", null, ct);
}
