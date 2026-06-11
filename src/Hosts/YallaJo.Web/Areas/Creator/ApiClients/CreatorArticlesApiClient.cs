using YallaJo.Web.Features.Blogs.Models;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Areas.Creator.Models.Dashboard;
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

    // ── Blog ↔ Tour links (CCD §7.2) — RowVersion-guarded ──────────────────────

    /// <summary>
    /// POST /api/v1/blogs/{id}/tours (BlogTourLink.Create). Links one tour to the blog;
    /// RowVersion required (ST1). The backend accepts a batch, but the editor links one
    /// tour at a time.
    /// </summary>
    public Task<ApiResult> LinkTourAsync(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
        => _api.PostAsync(
            $"/api/v1/blogs/{id}/tours",
            new BlogLinkToursRequestBody(rowVersion, [new BlogLinkTourItemBody(tourId)]),
            ct);

    /// <summary>
    /// DELETE /api/v1/blogs/{id}/tours/{tourId} (BlogTourLink.Delete). Removes one
    /// blog↔tour link; RowVersion in the body (ST1).
    /// </summary>
    public Task<ApiResult> UnlinkTourAsync(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
        => _api.DeleteAsync(
            $"/api/v1/blogs/{id}/tours/{tourId}",
            new BlogRowVersionRequestBody(rowVersion),
            ct);

    // ── Tour lookup (for the link combobox + chip name resolution) ─────────────

    /// <summary>
    /// GET /api/v1/tours/search/suggest?q= — anonymous tour autocomplete. Used to power
    /// the link combobox (F10: pick a tour by name, never paste a raw GUID) and to
    /// resolve chip display names.
    /// </summary>
    public Task<ApiResult<List<TourSuggestResponse>>> SuggestToursAsync(string q, CancellationToken ct = default)
        => _api.GetAsync<List<TourSuggestResponse>>(
            $"/api/v1/tours/search/suggest?q={Uri.EscapeDataString(q ?? string.Empty)}", ct);

    /// <summary>
    /// GET /api/v1/tours/{id} — single tour, used to resolve a linked-tour chip's display
    /// name when it is not already known. Callers batch these with a small fixed bound
    /// (Task.WhenAll over the few linked tours — API1; the bound keeps API7 satisfied).
    /// </summary>
    public Task<ApiResult<TourSuggestResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourSuggestResponse>($"/api/v1/tours/{id}", ct);

    /// <summary>
    /// GET /api/v1/blogs/my-blogs/status-counts (Blog.ReadOwn) — owner-scoped aggregate
    /// counts of own articles by lifecycle bucket. One small call powers the dashboard
    /// needs-attention strip and the Articles status tabs (API7: no per-status looping).
    /// </summary>
    public Task<ApiResult<MyBlogStatusCountsResponse>> GetMyStatusCountsAsync(CancellationToken ct = default)
        => _api.GetAsync<MyBlogStatusCountsResponse>("/api/v1/blogs/my-blogs/status-counts", ct);
}
