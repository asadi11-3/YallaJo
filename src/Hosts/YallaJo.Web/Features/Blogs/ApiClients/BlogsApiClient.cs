using YallaJo.Web.Features.Blogs.Models;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Features.Blogs.ApiClients;

public sealed class BlogsApiClient
{
    private readonly IApiClient _api;

    public BlogsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> ListBlogsAsync(
        int page, int pageSize, string? search, bool? isFeatured, CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (isFeatured.HasValue)
            query += $"&isFeatured={(isFeatured.Value ? "true" : "false")}";

        return _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>($"/api/v1/blogs{query}", ct);
    }

    public Task<ApiResult<BlogDetailResponse>> GetBlogBySlugAsync(string slug, CancellationToken ct = default)
        => _api.GetAsync<BlogDetailResponse>($"/api/v1/blogs/slug/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<PaginatedResponse<BlogCommentResponse>>> ListCommentsAsync(
        Guid blogId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<PaginatedResponse<BlogCommentResponse>>(
            $"/api/v1/blogs/{blogId}/comments?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<List<CreatorNicheResponse>>> ListCreatorNichesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<CreatorNicheResponse>>("/api/v1/blogs/creators/niches", ct);

    public Task<ApiResult<CreatorProfileResponse>> GetCreatorProfileBySlugAsync(
        string slug, CancellationToken ct = default)
        => _api.GetAsync<CreatorProfileResponse>(
            $"/api/v1/blogs/creators/profiles/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> GetCreatorBlogsAsync(
        string slug, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>(
            $"/api/v1/blogs/creators/profiles/{Uri.EscapeDataString(slug)}/blogs?page={page}&pageSize={pageSize}", ct);

    // --- Comment + reaction writes ---

    public Task<ApiResult> PostCommentAsync(
        Guid blogId, CreateBlogCommentRequestBody body, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{blogId}/comments", body, ct);

    public Task<ApiResult> AddReactionAsync(
        Guid commentId, BlogReactionRequestBody body, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/comments/{commentId}/reactions", body, ct);

    public Task<ApiResult> RemoveReactionAsync(Guid commentId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/blogs/comments/{commentId}/reactions", ct);

    // --- Follow / unfollow ---

    public Task<ApiResult> FollowAsync(Guid profileId, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/creators/profiles/{profileId}/follow", null, ct);

    public Task<ApiResult> UnfollowAsync(Guid profileId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/blogs/creators/profiles/{profileId}/follow", ct);

    // --- Composer (create / edit / submit) ---

    public Task<ApiResult<CreateBlogResultResponse>> CreateBlogAsync(
        CreateBlogRequestBody body, CancellationToken ct = default)
        => _api.PostAsync<CreateBlogResultResponse>("/api/v1/blogs", body, ct);

    public Task<ApiResult<AdminBlogDetailResponse>> GetAdminBlogAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminBlogDetailResponse>($"/api/v1/blogs/admin/{id}", ct);

    public Task<ApiResult> UpdateBlogAsync(Guid id, UpdateBlogRequestBody body, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/blogs/{id}", body, ct);

    /// <summary>
    /// GET /api/v1/blogs/my-blogs — own articles. Aligned to the real paginated
    /// contract (Gap 5): the backend returns PaginatedResult&lt;BlogSummaryDto&gt;,
    /// which now also carries Status + SourceLanguageCode (Gap 1). Supports optional
    /// paging/status filtering, matching the endpoint's parameters.
    /// </summary>
    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> ListMyBlogsAsync(
        int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        return _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>($"/api/v1/blogs/my-blogs{query}", ct);
    }

    /// <summary>
    /// POST /api/v1/blogs/{id}/submit-for-review. The backend requires a RowVersion
    /// (BlogRowVersionRequest); previously this posted a null body and always failed
    /// (Gap 5 fix). The caller obtains the verbatim base64 RowVersion from the
    /// admin-get prefetch.
    /// </summary>
    public Task<ApiResult> SubmitForReviewAsync(Guid id, string rowVersion, CancellationToken ct = default)
        => _api.PostAsync(
            $"/api/v1/blogs/{id}/submit-for-review",
            new BlogRowVersionRequestBody(rowVersion),
            ct);
}
