using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Agencies;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class AgenciesApiClient(IApiClient api)
{
    private const string Base = "/api/v1/agency";

    public Task<ApiResult<PaginatedAgenciesResponse>> GetAgenciesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<PaginatedAgenciesResponse>(url, ct);
    }

    public Task<ApiResult<AgencyDetailResponse>> GetAgencyAsync(Guid agencyUserId, CancellationToken ct = default)
        => api.GetAsync<AgencyDetailResponse>($"{Base}/{agencyUserId}", ct);

    public Task<ApiResult> ApplyAsync(Guid agencyUserId, CancellationToken ct = default)
        => api.PostAsync($"/api/v1/guides/agencies/{agencyUserId}/apply", null, ct);
}
