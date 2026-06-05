using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class DashboardApiClient
{
    private readonly IApiClient _api;

    public DashboardApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourGuideProfileResponse>> GetMyProfileAsync(CancellationToken ct = default)
        => _api.GetAsync<TourGuideProfileResponse>("/api/v1/guides/me", ct);

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetEarningsSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<GuideEarningsSummaryResponse>("/api/v1/guides/me/earnings/summary", ct);

    public Task<ApiResult<GuideToursResponse>> GetMyToursAsync(Guid guideId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<GuideToursResponse>($"/api/v1/guides/{guideId}/tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<List<GuideAvailabilityBlockResponse>>> GetAvailabilityBlocksAsync(CancellationToken ct = default)
        => _api.GetAsync<List<GuideAvailabilityBlockResponse>>("/api/v1/guides/me/availability-blocks", ct);
}
