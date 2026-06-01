using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags;

public sealed class TagsFacade
{
    private readonly TagsApiClient _api;
    private readonly IOutputCacheStore _cache;

    public TagsFacade(TagsApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<TagListVm>> GetTagsAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetTagsAsync(activeOnly, ct);
        if (result.IsSuccess)
            return ApiResult<TagListVm>.CreateSuccess(new TagListVm
            {
                Tags = (result.Data ?? []).Select(TagsMapper.ToRowVm).ToList(),
            });
        if (result.IsUnauthorized) return ApiResult<TagListVm>.ForceSignOut();
        return ApiResult<TagListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateTagVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.CreateAsync(TagsMapper.ToCreateRequest(vm), ct), "Could not create tag.", ct);

    public async Task<ApiResult> UpdateAsync(UpdateTagVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.UpdateAsync(vm.Id, TagsMapper.ToUpdateRequest(vm), ct), "Could not update tag.", ct);

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => await NormalizeAsync(await _api.DeleteAsync(id, ct), "Could not delete tag.", ct);

    // Evicts the shared "lookups" output-cache tag whenever a write succeeds.
    private async Task<ApiResult> NormalizeAsync(ApiResult result, string fallback, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync("lookups", ct);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("Conflict: a tag with this slug already exists.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
