using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class GuideApiClient
{
    private readonly IApiClient _api;

    public GuideApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourGuideProfileResponse>> GetMyProfileAsync(CancellationToken ct = default)
        => _api.GetAsync<TourGuideProfileResponse>("/api/v1/guides/me", ct);

    public Task<ApiResult> UpdateProfileAsync(Guid guideId, UpdateTourGuideProfileRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/guides/{guideId}", request, ct);

    public Task<ApiResult<GuideToursResponse>> GetGuideToursAsync(Guid guideId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<GuideToursResponse>($"/api/v1/guides/{guideId}/tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<List<GuideAvailabilityBlockResponse>>> GetAvailabilityBlocksAsync(CancellationToken ct = default)
        => _api.GetAsync<List<GuideAvailabilityBlockResponse>>("/api/v1/guides/me/availability-blocks", ct);

    public Task<ApiResult> AddAvailabilityBlockAsync(CreateGuideAvailabilityBlockRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/guides/me/availability-blocks", request, ct);

    public Task<ApiResult> DeleteAvailabilityBlockAsync(Guid blockId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/guides/me/availability-blocks/{blockId}", ct);

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetEarningsSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<GuideEarningsSummaryResponse>("/api/v1/guides/me/earnings/summary", ct);

    public Task<ApiResult<List<SpecializationResponse>>> GetSpecializationsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SpecializationResponse>>("/api/v1/content-core/specializations?activeOnly=true", ct);
}
