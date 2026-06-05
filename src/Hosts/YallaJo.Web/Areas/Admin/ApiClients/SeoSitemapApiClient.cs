using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.SeoSitemap;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class SeoSitemapApiClient
{
    private const string Base = "/api/v1/seo/sitemap";
    private readonly IApiClient _api;

    public SeoSitemapApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedSitemapResponse>> GetEntriesAsync(SitemapFilterRequest filter, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = filter.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query["entityType"] = filter.EntityType;
        }

        if (filter.IsActive.HasValue)
        {
            query["isActive"] = filter.IsActive.Value ? "true" : "false";
        }

        var url = QueryHelpers.AddQueryString($"{Base}/entries", query);
        return _api.GetAsync<PaginatedSitemapResponse>(url, ct);
    }

    public Task<ApiResult> UpdateAsync(Guid id, UpdateSitemapEntryApiRequest request, CancellationToken ct = default)
        => _api.PatchAsync($"{Base}/entries/{id:D}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/entries/{id:D}", ct);

    public Task<ApiResult<RegenerateSitemapResponse>> RegenerateAsync(CancellationToken ct = default)
        => _api.PostAsync<RegenerateSitemapResponse>($"{Base}/regenerate", null, ct);
}
