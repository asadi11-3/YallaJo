using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations;

public sealed class TranslationsFacade
{
    private readonly TranslationsApiClient _api;
    public TranslationsFacade(TranslationsApiClient api) => _api = api;

    public async Task<ApiResult<TranslationListVm>> GetForEntityAsync(
        TranslationFilterVm filter, CancellationToken ct = default)
    {
        var result = await _api.GetEntityTranslationsAsync(filter.EntityType.ToString(), filter.EntityId, ct);

        if (result.IsSuccess)
        {
            var rows = (result.Data ?? []).Select(TranslationsMapper.ToRowVm).ToList();
            return ApiResult<TranslationListVm>.CreateSuccess(new TranslationListVm
            {
                Filter       = filter,
                HasFilter    = true,
                Translations = rows,
            });
        }

        if (result.IsUnauthorized) return ApiResult<TranslationListVm>.ForceSignOut();
        return ApiResult<TranslationListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult<TranslateOnDemandVm>> TranslateOnDemandAsync(
        TranslateOnDemandVm vm, CancellationToken ct = default)
    {
        var result = await _api.TranslateAsync(TranslationsMapper.ToTranslateRequest(vm), ct);

        if (result.IsSuccess && result.Data is not null)
        {
            return ApiResult<TranslateOnDemandVm>.CreateSuccess(new TranslateOnDemandVm
            {
                Text             = vm.Text,
                FromLanguageCode = vm.FromLanguageCode,
                ToLanguageCode   = vm.ToLanguageCode,
                LastOriginal     = result.Data.OriginalText,
                LastTranslated   = result.Data.TranslatedText,
                LastConfidence   = result.Data.Confidence,
            });
        }

        if (result.IsUnauthorized)    return ApiResult<TranslateOnDemandVm>.ForceSignOut();
        if (result.IsValidationError) return ApiResult<TranslateOnDemandVm>.CreateValidationFailure(result.StatusCode, result.ValidationErrors!);
        return ApiResult<TranslateOnDemandVm>.CreateFailure(result.StatusCode, result.Error ?? "Translation failed.");
    }

    public async Task<ApiResult> UpdateAsync(UpdateTranslationVm vm, CancellationToken ct = default)
        => Normalize(await _api.UpdateAsync(vm.Id, TranslationsMapper.ToUpdateRequest(vm), ct),
            "Could not update translation.");

    public async Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => Normalize(await _api.ApproveAsync(id, ct), "Could not approve translation.");

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Translation not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
