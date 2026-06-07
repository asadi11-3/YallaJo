using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Public.Models.Blog;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

/// <summary>
/// Talks to the public blog + creator endpoints (group /api/v1/blogs). All routes used here are AllowAnonymous.
/// </summary>
public sealed class BlogApiClient(IApiClient api)
{
    private const string Base = "/api/v1/blogs";

    public Task<ApiResult<PaginatedBlogsResponse>> GetBlogsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(Base, new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<PaginatedBlogsResponse>(url, ct);
    }

    public Task<ApiResult<BlogDetailResponse>> GetBlogBySlugAsync(string slug, CancellationToken ct = default)
        => api.GetAsync<BlogDetailResponse>($"{Base}/slug/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult> RecordViewAsync(Guid id, CancellationToken ct = default)
        => api.PostAsync($"{Base}/{id}/views", null, ct);

    public Task<ApiResult<CreatorProfileResponse>> GetCreatorBySlugAsync(string slug, CancellationToken ct = default)
        => api.GetAsync<CreatorProfileResponse>($"{Base}/creators/profiles/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<PaginatedBlogsResponse>> GetCreatorBlogsAsync(string slug, int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(
            $"{Base}/creators/profiles/{Uri.EscapeDataString(slug)}/blogs",
            new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString(),
            });
        return api.GetAsync<PaginatedBlogsResponse>(url, ct);
    }

    public Task<ApiResult<BlogCommentPageResponse>> GetCommentsAsync(Guid postId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/{postId}/comments", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return api.GetAsync<BlogCommentPageResponse>(url, ct);
    }

    public Task<ApiResult> CreateCommentAsync(Guid postId, CreateBlogCommentBody body, CancellationToken ct = default)
        => api.PostAsync($"{Base}/{postId}/comments", body, ct);

    public Task<ApiResult> EditCommentAsync(Guid commentId, EditBlogCommentBody body, CancellationToken ct = default)
        => api.PutAsync($"{Base}/comments/{commentId}", body, ct);

    public Task<ApiResult> DeleteCommentAsync(Guid commentId, DeleteBlogCommentBody body, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/comments/{commentId}", body, ct);

    public Task<ApiResult> AddCommentReactionAsync(Guid commentId, CancellationToken ct = default)
        => api.PostAsync($"{Base}/comments/{commentId}/reactions", new AddBlogCommentReactionBody(), ct);

    public Task<ApiResult> RemoveCommentReactionAsync(Guid commentId, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/comments/{commentId}/reactions", ct);

    public async Task<ApiResult<FollowStateResponse>> GetFollowStateAsync(Guid profileId, CancellationToken ct = default)
    {
        // The backend returns a bare JSON boolean (`true`/`false`), so we
        // deserialize a bool and wrap it into FollowStateResponse to keep the
        // facade/controller contract unchanged.
        var result = await api.GetAsync<bool>($"{Base}/creators/profiles/{profileId}/following", ct);
        return result.IsSuccess
            ? ApiResult<FollowStateResponse>.Ok(new FollowStateResponse { IsFollowing = result.Data }, result.StatusCode)
            : ApiResult<FollowStateResponse>.Fail(result.StatusCode, result.Error);
    }

    public Task<ApiResult> FollowCreatorAsync(Guid profileId, CancellationToken ct = default)
        => api.PostAsync($"{Base}/creators/profiles/{profileId}/follow", null, ct);

    public Task<ApiResult> UnfollowCreatorAsync(Guid profileId, CancellationToken ct = default)
        => api.DeleteAsync($"{Base}/creators/profiles/{profileId}/follow", ct);
}
