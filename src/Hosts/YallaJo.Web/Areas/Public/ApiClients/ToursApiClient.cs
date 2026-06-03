using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class ToursApiClient
{
    private readonly IApiClient _api;

    public ToursApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedToursResponse>> GetToursAsync(
        int page, int pageSize, string sort, CancellationToken ct = default)
        => _api.GetAsync<PaginatedToursResponse>(
            $"/api/v1/tours?page={page}&pageSize={pageSize}&sort={Uri.EscapeDataString(sort)}", ct);

    public Task<ApiResult<List<CategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<CategoryResponse>>("/api/v1/content-core/categories", ct);

    public Task<ApiResult<TourDetailResponse>> GetTourBySlugAsync(string slug, CancellationToken ct = default)
        => _api.GetAsync<TourDetailResponse>($"/api/v1/tours/slug/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<List<TourScheduleResponse>>> GetSchedulesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourScheduleResponse>>($"/api/v1/tours/{id}/schedules?activeOnly=true", ct);

    public Task<ApiResult<List<TourPricingTierResponse>>> GetPricingAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourPricingTierResponse>>($"/api/v1/tours/{id}/pricing?activeOnly=true", ct);

    public Task<ApiResult<List<TourWaypointResponse>>> GetWaypointsAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourWaypointResponse>>($"/api/v1/tours/{id}/waypoints", ct);

    public Task<ApiResult<List<TourGuideResponse>>> GetGuidesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourGuideResponse>>($"/api/v1/tours/{id}/guides", ct);

    public Task<ApiResult<PublicReviewPageResponse>> GetReviewsAsync(Guid id, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<PublicReviewPageResponse>($"/api/v1/social/reviews/Tour/{id}?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<RatingSummaryResponse>> GetRatingSummaryAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<RatingSummaryResponse>($"/api/v1/social/reviews/ratings?entityType=Tour&entityId={id}", ct);

    public Task<ApiResult<AvailabilityPageResponse>> GetAvailabilityAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<AvailabilityPageResponse>($"/api/v1/booking/availability/{tourId}?pageSize=20", ct);

    public Task<ApiResult> SubmitJoinRequestAsync(SubmitJoinRequestBody body, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/booking/join-requests", body, ct);
}
