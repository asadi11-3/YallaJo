using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class TagsApiClient
{
    private readonly IApiClient _api;
    public TagsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<TagItemResponse>>> GetTagsAsync(
        bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<TagItemResponse>>(
            $"/api/v1/content-core/tags?activeOnly={(activeOnly ? "true" : "false")}", ct);

    public Task<ApiResult> CreateAsync(CreateTagRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/tags", request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateTagRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/tags/{id}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/content-core/tags/{id}", ct);
}
