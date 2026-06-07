using YallaJo.Web.Areas.Admin.Models.Translations;
using YallaJo.Web.Areas.Admin.Models.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;

using YallaJo.Web.Areas.Admin.ApiClients;
namespace YallaJo.Web.Areas.Admin.Facades;

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

    // §8.9 — backfill missing translations for all Tag or Specialization rows.
    public async Task<ApiResult> BackfillAsync(string? entityKind, CancellationToken ct = default)
    {
        var kind = (entityKind ?? string.Empty).Trim().ToLowerInvariant();
        if (kind is not ("tag" or "specialization"))
        {
            return ApiResult.Fail(400, "Entity kind must be 'tag' or 'specialization'.");
        }

        return Normalize(await _api.BackfillAsync(kind, ct), "Could not start the translation backfill.");
    }

    // §8.9 — mark all auto-translated fields reviewed for an entity + language.
    public async Task<ApiResult> ApproveBatchAsync(
        string? entityType, Guid entityId, string? languageCode, IReadOnlyList<string>? fieldNames, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty || string.IsNullOrWhiteSpace(languageCode))
        {
            return ApiResult.Fail(400, "Entity type, entity id, and language code are required.");
        }

        var request = new
        {
            entityType = entityType.Trim(),
            entityId,
            languageCode = languageCode.Trim(),
            fieldNames = (fieldNames ?? []).Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim()).ToList(),
        };
        return Normalize(await _api.ApproveBatchAsync(request, ct), "Could not approve the translations.");
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Translation not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
