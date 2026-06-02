using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class TagsApiClient
{
    private readonly IApiClient _api;

    public TagsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<TagResponse>>> ListTagsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<TagResponse>>("/api/v1/content-core/tags", ct);
}
