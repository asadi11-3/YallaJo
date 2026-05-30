using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages;

public sealed class LanguagesFacade
{
    private readonly LanguagesApiClient _api;
    public LanguagesFacade(LanguagesApiClient api) => _api = api;

    public async Task<ApiResult<LanguageListVm>> GetLanguagesAsync(
        bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetLanguagesAsync(activeOnly, ct);

        if (result.IsSuccess)
        {
            var vm = new LanguageListVm
            {
                Languages  = (result.Data ?? []).Select(LanguagesMapper.ToRowVm).ToList(),
                ActiveOnly = activeOnly,
            };
            return ApiResult<LanguageListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<LanguageListVm>.ForceSignOut();
        return ApiResult<LanguageListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateLanguageVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateAsync(LanguagesMapper.ToCreateRequest(vm), ct);
        return Normalize(result, "Could not create language.");
    }

    public async Task<ApiResult> UpdateAsync(UpdateLanguageVm vm, CancellationToken ct = default)
    {
        var result = await _api.UpdateAsync(vm.Id, LanguagesMapper.ToUpdateRequest(vm), ct);
        return Normalize(result, "Could not update language.");
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("A language with this code already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Language not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
