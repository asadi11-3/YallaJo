using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>Reads the logged-in guide's profile id and the public reviews/ratings about that guide.</summary>
public sealed class ReviewsApiClient
{
    private const string GuidesBase = "/api/v1/guides";
    private const string ReviewsBase = "/api/v1/social/reviews";
    private const string GuideTargetType = "TourGuide";

    private readonly IApiClient _api;

    public ReviewsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourGuideProfileResponse>> GetMyProfileAsync(CancellationToken ct = default) =>
        _api.GetAsync<TourGuideProfileResponse>($"{GuidesBase}/me", ct);

    public Task<ApiResult<PublicReviewPageResponse>> GetReviewsAsync(Guid guideId, int page, int pageSize, CancellationToken ct = default) =>
        _api.GetAsync<PublicReviewPageResponse>(
            $"{ReviewsBase}?entityType={GuideTargetType}&entityId={guideId}&page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<RatingSummaryResponse>> GetRatingSummaryAsync(Guid guideId, CancellationToken ct = default) =>
        _api.GetAsync<RatingSummaryResponse>(
            $"{ReviewsBase}/ratings?entityType={GuideTargetType}&entityId={guideId}", ct);
}
