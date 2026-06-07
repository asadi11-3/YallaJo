using YallaJo.Web.Areas.Provider.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ReviewsApiClient
{
    private readonly IApiClient _api;

    public ReviewsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<MyToursResponse>> GetMyToursAsync(CancellationToken ct = default) =>
        _api.GetAsync<MyToursResponse>("/api/v1/tours/provider/my-tours?page=1&pageSize=100", ct);

    public Task<ApiResult<PublicReviewPageResponse>> GetReviewsAsync(Guid tourId, int page, int pageSize, CancellationToken ct = default) =>
        _api.GetAsync<PublicReviewPageResponse>($"/api/v1/social/reviews/Tour/{tourId}?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<RatingSummaryResponse>> GetRatingAsync(Guid tourId, CancellationToken ct = default) =>
        _api.GetAsync<RatingSummaryResponse>($"/api/v1/social/reviews/ratings?entityType=Tour&entityId={tourId}", ct);

    public Task<ApiResult> ReplyAsync(Guid reviewId, AddReplyRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/social/reviews/{reviewId}/reply", request, ct);

    public Task<ApiResult> UpdateReplyAsync(Guid reviewId, Guid replyId, AddReplyRequest request, CancellationToken ct = default) =>
        _api.PutAsync($"/api/v1/social/reviews/{reviewId}/reply/{replyId}", request, ct);

    public Task<ApiResult> DeleteReplyAsync(Guid reviewId, Guid replyId, CancellationToken ct = default) =>
        _api.DeleteAsync($"/api/v1/social/reviews/{reviewId}/reply/{replyId}", ct);

    public Task<ApiResult> ReportAsync(Guid reviewId, ReviewReportRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/social/reviews/{reviewId}/report", request, ct);
}
