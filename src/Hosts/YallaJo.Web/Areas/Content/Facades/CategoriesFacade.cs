using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class CategoriesFacade
{
    private readonly CategoriesApiClient _api;

    public CategoriesFacade(CategoriesApiClient api) => _api = api;

    public async Task<ApiResult<List<CategoryBadgeVm>>> GetBadgesAsync(CancellationToken ct = default)
    {
        var result = await _api.ListCategoriesAsync(ct);

        if (result.IsSuccess)
        {
            var badges = (result.Data ?? []).Select(BlogsMapper.ToCategoryBadgeVm).ToList();
            return ApiResult<List<CategoryBadgeVm>>.Ok(badges);
        }

        if (result.IsUnauthorized) return ApiResult<List<CategoryBadgeVm>>.ForceSignOut();
        return ApiResult<List<CategoryBadgeVm>>.Fail(result.StatusCode, result.Error);
    }
}
