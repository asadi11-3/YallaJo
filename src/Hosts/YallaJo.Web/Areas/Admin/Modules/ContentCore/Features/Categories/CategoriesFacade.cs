using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories;

public sealed class CategoriesFacade
{
    private readonly CategoriesApiClient _api;
    private readonly IOutputCacheStore _cache;

    public CategoriesFacade(CategoriesApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<CategoryListVm>> GetCategoriesAsync(bool includeInactive, CancellationToken ct = default)
    {
        var result = await _api.GetCategoriesAsync(includeInactive, ct);
        if (result.IsSuccess)
            return ApiResult<CategoryListVm>.CreateSuccess(new CategoryListVm
            {
                Categories = CategoriesMapper.Flatten(result.Data ?? []),
            });
        if (result.IsUnauthorized) return ApiResult<CategoryListVm>.ForceSignOut();
        return ApiResult<CategoryListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult<UpdateCategoryVm>> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetAsync(id, ct);
        if (result.IsSuccess && result.Data is not null)
        {
            return ApiResult<UpdateCategoryVm>.CreateSuccess(new UpdateCategoryVm
            {
                Id               = result.Data.Id,
                Name             = result.Data.Name,
                Slug             = result.Data.Slug,
                Icon             = result.Data.Icon,
                SortOrder        = result.Data.SortOrder,
                ParentCategoryId = result.Data.ParentCategoryId,
            });
        }
        if (result.IsUnauthorized) return ApiResult<UpdateCategoryVm>.ForceSignOut();
        if (result.IsNotFound)     return ApiResult<UpdateCategoryVm>.CreateFailure(404, "Category not found.");
        return ApiResult<UpdateCategoryVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateCategoryVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.CreateAsync(CategoriesMapper.ToCreateRequest(vm), ct), "Could not create category.", ct);

    public async Task<ApiResult> UpdateAsync(UpdateCategoryVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.UpdateAsync(vm.Id, CategoriesMapper.ToUpdateRequest(vm), ct), "Could not update category.", ct);

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => await NormalizeAsync(await _api.DeleteAsync(id, ct), "Could not delete category.", ct);

    public async Task<ApiResult> DeactivateAsync(Guid id, CancellationToken ct = default)
        => await NormalizeAsync(await _api.DeactivateAsync(id, ct), "Could not deactivate category.", ct);

    public async Task<ApiResult> ActivateAsync(Guid id, CancellationToken ct = default)
        => await NormalizeAsync(await _api.ActivateAsync(id, ct), "Could not activate category.", ct);

    // Evicts the shared "lookups" output-cache tag whenever a write succeeds, so
    // any cached anonymous/public lookup reads reflect the change immediately.
    private async Task<ApiResult> NormalizeAsync(ApiResult result, string fallback, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync("lookups", ct);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("Conflict: a category with this slug already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Category not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
