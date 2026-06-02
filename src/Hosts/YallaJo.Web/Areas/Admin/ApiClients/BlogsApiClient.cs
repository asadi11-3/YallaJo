using YallaJo.Web.Areas.Admin.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Typed access to the existing ContentBlogs API for the admin blog screens.
/// Knows endpoint URLs only; all calls go through <see cref="IApiClient"/> and
/// return <see cref="ApiResult"/>/<see cref="ApiResult{T}"/>.
/// </summary>
public sealed class BlogsApiClient
{
    private readonly IApiClient _api;

    public BlogsApiClient(IApiClient api) => _api = api;

    // ── Lists ────────────────────────────────────────────────────────────────────

    // GET /api/v1/blogs  (Published only)
    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> ListPublishedAsync(
        int page, int pageSize, string? search, bool? isFeatured, CancellationToken ct = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (isFeatured.HasValue)
            query += $"&isFeatured={(isFeatured.Value ? "true" : "false")}";

        return _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>($"/api/v1/blogs{query}", ct);
    }

    // GET /api/v1/blogs/admin/queue  (PendingReview)
    public Task<ApiResult<PaginatedResponse<BlogSummaryResponse>>> ListQueueAsync(
        int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<PaginatedResponse<BlogSummaryResponse>>(
            $"/api/v1/blogs/admin/queue?page={page}&pageSize={pageSize}", ct);

    // GET /api/v1/blogs/admin/deleted  (soft-deleted)
    public Task<ApiResult<PaginatedResponse<AdminDeletedBlogResponse>>> ListDeletedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = $"?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&q={Uri.EscapeDataString(search)}";

        return _api.GetAsync<PaginatedResponse<AdminDeletedBlogResponse>>(
            $"/api/v1/blogs/admin/deleted{query}", ct);
    }

    // GET /api/v1/blogs/admin/{id}  (full detail incl. RowVersion + Status)
    public Task<ApiResult<AdminBlogDetailResponse>> GetAdminByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<AdminBlogDetailResponse>($"/api/v1/blogs/admin/{id}", ct);

    // ── Create / Update / Delete ──────────────────────────────────────────────────

    // POST /api/v1/blogs
    public Task<ApiResult<CreateBlogResponse>> CreateAsync(CreateBlogRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateBlogResponse>("/api/v1/blogs", request, ct);

    // PUT /api/v1/blogs/{id}
    public Task<ApiResult> UpdateAsync(Guid id, UpdateBlogRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/blogs/{id}", request, ct);

    // DELETE /api/v1/blogs/{id}  (body carries RowVersion)
    public Task<ApiResult> DeleteAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/blogs/{id}", request, ct);

    // ── Status actions (all POST with RowVersion body) ────────────────────────────

    public Task<ApiResult> RestoreAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/restore", request, ct);

    public Task<ApiResult> PublishAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/publish", request, ct);

    public Task<ApiResult> UnpublishAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/unpublish", request, ct);

    public Task<ApiResult> ArchiveAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/archive", request, ct);

    public Task<ApiResult> FeatureAsync(Guid id, FeatureBlogRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/feature", request, ct);

    public Task<ApiResult> UnfeatureAsync(Guid id, BlogRowVersionRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/blogs/{id}/unfeature", request, ct);
}
