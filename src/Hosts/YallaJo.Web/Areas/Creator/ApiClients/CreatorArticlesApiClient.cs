using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Creator.ApiClients;

/// <summary>
/// Typed client for the creator article-authoring endpoints used by CCD-4.
/// Registered automatically by <c>AddFeatureServices()</c> (name ends in "ApiClient").
/// <para>
/// This client supplies the methods the existing <c>Content.BlogsApiClient</c> lacks
/// or implements incorrectly for creators:
/// <list type="bullet">
///   <item>paged + status-filtered my-blogs (Content's version had no paging/status),</item>
///   <item>submit-for-review WITH RowVersion (Content's version POSTed a null body),</item>
///   <item>delete and restore (absent from Content's client).</item>
/// </list>
/// Create / admin-get / update are reused from <c>Content.BlogsApiClient</c>.
/// </para>
/// </summary>
public sealed class CreatorArticlesApiClient
{
    private readonly IApiClient _api;

    public CreatorArticlesApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// GET /api/v1/blogs/my-blogs?page&amp;pageSize&amp;status — own articles (Blog.ReadOwn).
    /// <paramref name="status"/> is a BlogStatus name; the backend ignores unknown values.
    /// </summary>
    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> ListMyArticlesAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        return _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>($"/api/v1/blogs/my-blogs{query}", ct);
    }

    /// <summary>
    /// POST /api/v1/blogs/{id}/submit-for-review (Blog.Submit). Requires RowVersion;
    /// strict self-only; Draft/Rejected → PendingReview (422 otherwise).
    /// </summary>
    public Task<ApiResult> SubmitForReviewAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/submit-for-review", new BlogRowVersionRequestBody(rowVersion), ct);

    /// <summary>
    /// DELETE /api/v1/blogs/{id} (Blog.DeleteOwn). Soft-delete; RowVersion in the body.
    /// </summary>
    public Task<ApiResult> DeleteArticleAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/blogs/{id}", new BlogRowVersionRequestBody(rowVersion), ct);

    /// <summary>
    /// POST /api/v1/blogs/{id}/restore (Blog.DeleteOwn). Restores a soft-deleted article;
    /// RowVersion required. CCD-4 only exposes this as an immediate post-delete Undo.
    /// </summary>
    public Task<ApiResult> RestoreArticleAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/restore", new BlogRowVersionRequestBody(rowVersion), ct);
}
