using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class GuidesApiClient(IApiClient api)
{
    private const string Base = "/api/v1/guides";

    public Task<ApiResult<PaginatedGuidesResponse>> GetGuidesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<PaginatedGuidesResponse>(url, ct);
    }

    public Task<ApiResult<GuideDetailResponse>> GetGuideBySlugAsync(string slug, CancellationToken ct = default) =>
        api.GetAsync<GuideDetailResponse>($"{Base}/by-slug/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<GuideToursResponse>> GetGuideToursAsync(Guid id, int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/{id}/tours", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<GuideToursResponse>(url, ct);
    }
}
