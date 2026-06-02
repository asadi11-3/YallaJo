using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class CategoriesApiClient
{
    private readonly IApiClient _api;

    public CategoriesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<CategoryResponse>>> ListCategoriesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<CategoryResponse>>("/api/v1/content-core/categories", ct);
}
