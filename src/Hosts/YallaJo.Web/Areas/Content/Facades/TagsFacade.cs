using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class TagsFacade
{
    private readonly TagsApiClient _api;

    public TagsFacade(TagsApiClient api) => _api = api;

    public async Task<ApiResult<List<TagVm>>> GetPopularTagsAsync(int take = 12, CancellationToken ct = default)
    {
        var result = await _api.ListTagsAsync(ct);

        if (result.IsSuccess)
        {
            var tags = (result.Data ?? [])
                .Where(t => t.IsActive)
                .Take(take)
                .Select(BlogsMapper.ToTagVm)
                .ToList();
            return ApiResult<List<TagVm>>.Ok(tags);
        }

        if (result.IsUnauthorized) return ApiResult<List<TagVm>>.ForceSignOut();
        return ApiResult<List<TagVm>>.Fail(result.StatusCode, result.Error);
    }
}
