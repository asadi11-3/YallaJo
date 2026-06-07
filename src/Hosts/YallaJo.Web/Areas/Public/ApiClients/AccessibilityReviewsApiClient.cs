using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

/// <summary>
/// Typed access to the accessibility-review endpoints
/// (<c>/api/v1/social/accessibility/reviews</c>). DISTINCT from <see cref="ReviewsApiClient"/>
/// (normal reviews, <c>/api/v1/social/reviews</c>): these carry a FeatureTypesCsv, no
/// RowVersion, and DELETE is bodyless.
/// </summary>
public sealed class AccessibilityReviewsApiClient(IApiClient api)
{
    private const string Base = "/api/v1/social/accessibility/reviews";

    // GET /api/v1/social/accessibility/reviews?entityType=&entityId=&page=&pageSize=
    public Task<ApiResult<PublicAccessibilityReviewPageResponse>> GetForEntityAsync(
        string entityType, Guid entityId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["entityType"] = entityType,
            ["entityId"] = entityId.ToString(),
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<PublicAccessibilityReviewPageResponse>(url, ct);
    }

    // GET /api/v1/social/accessibility/reviews/my?cursor=&pageSize=
    public Task<ApiResult<MyAccessibilityReviewPageResponse>> GetMyAsync(
        string? cursor, int pageSize, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["pageSize"] = pageSize.ToString() };
        if (!string.IsNullOrWhiteSpace(cursor)) query["cursor"] = cursor;
        var url = QueryHelpers.AddQueryString($"{Base}/my", query);
        return api.GetAsync<MyAccessibilityReviewPageResponse>(url, ct);
    }

    // POST /api/v1/social/accessibility/reviews
    public Task<ApiResult> CreateAsync(CreateAccessibilityReviewBody body, CancellationToken ct = default)
        => api.PostAsync(Base, body, ct);

    // PUT /api/v1/social/accessibility/reviews/{id}
    public Task<ApiResult> EditAsync(Guid id, UpdateAccessibilityReviewBody body, CancellationToken ct = default)
        => api.PutAsync($"{Base}/{id}", body, ct);

    // DELETE /api/v1/social/accessibility/reviews/{id} — bodyless (no RowVersion)
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/{id}", ct);
}
