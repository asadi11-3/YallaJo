using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class PackagesApiClient
{
    private const string Base = "/api/v1/tours/packages";

    private readonly IApiClient _api;

    public PackagesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/packages
    public Task<ApiResult<PackageListResponse>> GetPackagesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return _api.GetAsync<PackageListResponse>(url, ct);
    }

    // GET /api/v1/tours/packages/{id}
    public Task<ApiResult<PackageDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PackageDetailResponse>($"{Base}/{id}", ct);

    // POST /api/v1/tours/packages
    public Task<ApiResult> CreateAsync(CreateTourPackageApiRequest request, CancellationToken ct = default)
        => _api.PostAsync(Base, request, ct);

    // POST /api/v1/tours/packages/{id}/inclusions
    public Task<ApiResult> AddInclusionAsync(Guid id, AddPackageInclusionApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/inclusions", request, ct);

    // POST /api/v1/tours/packages/{id}/submit
    public Task<ApiResult> SubmitAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/submit", body: null, ct);

    // DELETE /api/v1/tours/packages/{id}  (soft-delete; no RowVersion per backend Decision #6)
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id}", ct);
}
