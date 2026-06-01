using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages;

public sealed class LanguagesFacade
{
    private readonly LanguagesApiClient _api;
    private readonly IOutputCacheStore _cache;

    public LanguagesFacade(LanguagesApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<LanguageListVm>> GetLanguagesAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetLanguagesAsync(activeOnly, ct);
        if (result.IsSuccess)
            return ApiResult<LanguageListVm>.CreateSuccess(new LanguageListVm
            {
                Languages = (result.Data ?? []).Select(LanguagesMapper.ToRowVm).ToList(),
            });
        if (result.IsUnauthorized) return ApiResult<LanguageListVm>.ForceSignOut();
        return ApiResult<LanguageListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateLanguageVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.CreateAsync(LanguagesMapper.ToCreateRequest(vm), ct), "Could not create language.", ct);

    public async Task<ApiResult> UpdateAsync(UpdateLanguageVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.UpdateAsync(vm.Id, LanguagesMapper.ToUpdateRequest(vm), ct), "Could not update language.", ct);

    // Evicts the shared "lookups" output-cache tag whenever a write succeeds.
    private async Task<ApiResult> NormalizeAsync(ApiResult result, string fallback, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync("lookups", ct);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("Conflict: a language with this code already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Language not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
