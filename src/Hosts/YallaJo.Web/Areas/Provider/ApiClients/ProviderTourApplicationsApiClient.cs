using YallaJo.Web.Areas.Provider.Models.TourApplications;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourApplicationsApiClient
{
    private readonly IApiClient _api;

    public ProviderTourApplicationsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/{tourId}/applications
    public Task<ApiResult<ListGuideApplicationsResponse>> GetApplicationsAsync(
        Guid tourId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<ListGuideApplicationsResponse>(
            $"/api/v1/tours/{tourId}/applications?page={page}&pageSize={pageSize}", ct);

    // POST /api/v1/tours/{tourId}/applications/{applicationId}/approve
    public Task<ApiResult> ApproveAsync(Guid tourId, Guid applicationId, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{tourId}/applications/{applicationId}/approve", new { }, ct);

    // POST /api/v1/tours/{tourId}/applications/{applicationId}/reject
    public Task<ApiResult> RejectAsync(
        Guid tourId, Guid applicationId, RejectGuideApplicationApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{tourId}/applications/{applicationId}/reject", request, ct);

    // POST /api/v1/tours/{tourId}/open-applications
    public Task<ApiResult> OpenApplicationsAsync(Guid tourId, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{tourId}/open-applications", new { }, ct);

    // POST /api/v1/tours/{tourId}/close-applications
    public Task<ApiResult> CloseApplicationsAsync(Guid tourId, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{tourId}/close-applications", new { }, ct);
}
