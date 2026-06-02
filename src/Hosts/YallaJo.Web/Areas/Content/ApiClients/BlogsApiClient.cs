using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

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
}
