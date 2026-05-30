using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags;

public sealed class TagsFacade
{
    private readonly TagsApiClient _api;
    public TagsFacade(TagsApiClient api) => _api = api;

    public async Task<ApiResult<TagListVm>> GetTagsAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetTagsAsync(activeOnly, ct);

        if (result.IsSuccess)
        {
            var vm = new TagListVm
            {
                Tags       = (result.Data ?? []).Select(TagsMapper.ToRowVm).ToList(),
                ActiveOnly = activeOnly,
            };
            return ApiResult<TagListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<TagListVm>.ForceSignOut();
        return ApiResult<TagListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateTagVm vm, CancellationToken ct = default)
        => Normalize(await _api.CreateAsync(TagsMapper.ToCreateRequest(vm), ct), "Could not create tag.");

    public async Task<ApiResult> UpdateAsync(UpdateTagVm vm, CancellationToken ct = default)
        => Normalize(await _api.UpdateAsync(vm.Id, TagsMapper.ToUpdateRequest(vm), ct), "Could not update tag.");

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(await _api.DeleteAsync(id, ct), "Could not delete tag.");

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("A tag with this name or slug already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Tag not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
