using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

/// <summary>Reads public tour-package endpoints (GET /api/v1/tours/packages[/{id}]).</summary>
public sealed class PackagesApiClient(IApiClient api)
{
    private const string Base = "/api/v1/tours/packages";

    public Task<ApiResult<PaginatedPackagesResponse>> GetPackagesAsync(
        int page,
        int pageSize,
        string? sort = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(sort))
        {
            query["sort"] = sort;
        }

        if (minPrice is { } min)
        {
            query["minPrice"] = min.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (maxPrice is { } max)
        {
            query["maxPrice"] = max.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var url = QueryHelpers.AddQueryString(Base, query);
        return api.GetAsync<PaginatedPackagesResponse>(url, ct);
    }

    public Task<ApiResult<PackageDetailResponse>> GetPackageAsync(Guid id, CancellationToken ct = default) =>
        api.GetAsync<PackageDetailResponse>($"{Base}/{id}", ct);
}
