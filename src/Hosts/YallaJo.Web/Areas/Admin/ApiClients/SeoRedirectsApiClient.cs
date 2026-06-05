using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.SeoRedirects;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class SeoRedirectsApiClient
{
    private const string Base = "/api/v1/seo/redirects";
    private readonly IApiClient _api;

    public SeoRedirectsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedRedirectsResponse>> GetRedirectsAsync(
        RedirectsFilterRequest filter,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = filter.PageSize.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(filter.OldUrl))
        {
            query["oldUrl"] = filter.OldUrl;
        }

        if (filter.IsActive.HasValue)
        {
            query["isActive"] = filter.IsActive.Value ? "true" : "false";
        }

        if (filter.StatusCode.HasValue)
        {
            query["statusCode"] = filter.StatusCode.Value.ToString(CultureInfo.InvariantCulture);
        }

        var url = QueryHelpers.AddQueryString(Base, query);
        return _api.GetAsync<PaginatedRedirectsResponse>(url, ct);
    }

    public Task<ApiResult> CreateAsync(CreateRedirectApiRequest request, CancellationToken ct = default)
        => _api.PostAsync(Base, request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateRedirectApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id:D}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id:D}", ct);
}
