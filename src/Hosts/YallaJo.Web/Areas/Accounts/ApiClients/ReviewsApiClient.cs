using YallaJo.Web.Areas.Accounts.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class ReviewsApiClient
{
    private const string Base = "/api/v1/social/reviews";

    private readonly IApiClient _api;

    public ReviewsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/social/reviews/my-reviews?cursor&pageSize
    public Task<ApiResult<ReviewPageResponse>> GetMyReviewsAsync(
        string? cursor = null, int pageSize = 20, CancellationToken ct = default)
    {
        var path = $"{Base}/my-reviews?pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(cursor))
            path += $"&cursor={Uri.EscapeDataString(cursor)}";
        return _api.GetAsync<ReviewPageResponse>(path, ct);
    }

    // POST /api/v1/social/reviews
    public Task<ApiResult<Guid>> CreateAsync(CreateReviewRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>(Base, request, ct);

    // PUT /api/v1/social/reviews/{id}
    public Task<ApiResult> EditAsync(Guid id, EditReviewRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id}", request, ct);

    // DELETE /api/v1/social/reviews/{id} (body carries the concurrency token)
    public Task<ApiResult> DeleteAsync(Guid id, DeleteReviewRequest request, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id}", request, ct);

    // POST /api/v1/social/reviews/{id}/helpful
    public Task<ApiResult> MarkHelpfulAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/helpful", null, ct);

    // DELETE /api/v1/social/reviews/{id}/helpful
    public Task<ApiResult> UnmarkHelpfulAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id}/helpful", ct);
}
