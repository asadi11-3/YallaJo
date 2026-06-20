using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class ReviewsApiClient(IApiClient api)
{
    private const string Base = "/api/v1/social";

    public Task<ApiResult> CreateReviewAsync(CreateReviewBody body, CancellationToken ct = default)
        => api.PostAsync($"{Base}/reviews", body, ct);

    // Creates a review and returns the new review id (API returns the Guid id with HTTP 200).
    public Task<ApiResult<Guid>> CreateReviewWithIdAsync(CreateReviewBody body, CancellationToken ct = default)
        => api.PostAsync<Guid>($"{Base}/reviews", body, ct);

    // Uploads a single review image to the shared ContentCore attachment pipeline (EntityType=Review).
    // ContentCore enforces magic-byte validation, allowed types, size cap and the per-entity limit (5).
    public Task<ApiResult<UploadReviewImageResponse>> UploadImageAsync(
        Guid reviewId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
        => api.PostFileAsync<UploadReviewImageResponse>(
            "/api/v1/content-core/attachments",
            fileStream,
            fileName,
            contentType,
            formFields: new Dictionary<string, string>
            {
                ["EntityType"] = "Review",
                ["EntityId"] = reviewId.ToString(),
                ["AttachmentType"] = "Image",
            },
            formFieldName: "file",
            ct: ct);

    public Task<ApiResult> EditReviewAsync(Guid id, EditReviewBody body, CancellationToken ct = default)
        => api.PutAsync($"{Base}/reviews/{id}", body, ct);

    public Task<ApiResult> DeleteReviewAsync(Guid id, DeleteReviewBody body, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/reviews/{id}", body, ct);

    public Task<ApiResult> MarkHelpfulAsync(Guid id, CancellationToken ct = default)
        => api.PostAsync($"{Base}/reviews/{id}/helpful", null, ct);

    public Task<ApiResult> UnmarkHelpfulAsync(Guid id, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/reviews/{id}/helpful", ct);

    public Task<ApiResult> ReportAsync(SubmitReportBody body, CancellationToken ct = default)
        => api.PostAsync($"{Base}/reports", body, ct);

    public Task<ApiResult> ReportReviewAsync(Guid id, ReviewReportBody body, CancellationToken ct = default)
        => api.PostAsync($"{Base}/reviews/{id}/report", body, ct);

    public Task<ApiResult> ReportEntityAsync(SubmitReportBody body, CancellationToken ct = default)
        => api.PostAsync($"{Base}/reports", body, ct);

    public Task<ApiResult<PublicReviewPageResponse>> GetReviewsAsync(string targetType, Guid targetId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/reviews/{targetType}/{targetId}", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<PublicReviewPageResponse>(url, ct);
    }

    public Task<ApiResult<RatingSummaryResponse>> GetRatingsAsync(string targetType, Guid targetId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/reviews/ratings", new Dictionary<string, string?>
        {
            ["entityType"] = targetType,
            ["entityId"] = targetId.ToString(),
        });
        return api.GetAsync<RatingSummaryResponse>(url, ct);
    }

    // Caller-specific review eligibility (requires authentication). Drives UI gating of the
    // create-review form so an ineligible user is never shown a form the API will always reject.
    public Task<ApiResult<ReviewEligibilityResponse>> GetEligibilityAsync(string targetType, Guid targetId, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/reviews/eligibility", new Dictionary<string, string?>
        {
            ["entityType"] = targetType,
            ["entityId"] = targetId.ToString(),
        });
        return api.GetAsync<ReviewEligibilityResponse>(url, ct);
    }
}
