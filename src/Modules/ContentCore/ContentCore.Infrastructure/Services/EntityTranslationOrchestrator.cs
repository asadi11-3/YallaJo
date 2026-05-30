using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Batch-translates a set of entity fields to one or more target languages
/// using <see cref="ITranslationService"/> (which itself is decorated with
/// <see cref="AutoSaveTranslationService"/> caching).
/// </summary>
internal sealed class EntityTranslationOrchestrator(
    ITranslationService translationService,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<EntityTranslationOrchestrator> logger) : IEntityTranslationOrchestrator
{
    public async Task<IReadOnlyList<EntityFieldTranslationSet>> TranslateAsync(
        IReadOnlyDictionary<string, string> fields,
        string sourceLanguageCode,
        IReadOnlyList<string> targetLanguageCodes,
        CancellationToken ct = default)
    {
        if (fields.Count == 0 || targetLanguageCodes.Count == 0)
            return [];

        var fieldNames = fields.Keys.ToList();
        var fieldValues = fields.Values.ToList();
        var results = new List<EntityFieldTranslationSet>(targetLanguageCodes.Count);

        // We need LanguageId for each target code.
        // Resolve all active languages once so we can map code → Guid.
        var activeLanguages = await activeLanguageProvider.GetActiveLanguagesAsync(ct);
        var codeLookup = activeLanguages.ToDictionary(l => l.Code, l => l.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var targetCode in targetLanguageCodes)
        {
            if (string.Equals(targetCode, sourceLanguageCode, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!codeLookup.TryGetValue(targetCode, out var languageId))
            {
                logger.LogWarning(
                    "Target language '{Code}' is not registered or not active — skipping",
                    targetCode);
                continue;
            }

            // Batch-translate all field values in a single API call per language
            var translateResult = await translationService.BatchTranslateAsync(
                fieldValues, sourceLanguageCode, targetCode, ct);

            if (translateResult.IsFailure)
            {
                logger.LogWarning(
                    "Translation failed for language '{Code}': {Errors} — skipping",
                    targetCode, string.Join("; ", translateResult.Errors.Select(e => e.Message)));
                continue;
            }

            var translated = translateResult.Value;
            var translatedFields = new Dictionary<string, string>(fieldNames.Count, StringComparer.Ordinal);
            for (var i = 0; i < fieldNames.Count; i++)
            {
                translatedFields[fieldNames[i]] = translated[i].TranslatedText;
            }

            results.Add(new EntityFieldTranslationSet(languageId, targetCode, translatedFields));
        }

        logger.LogDebug(
            "Entity field translation: {FieldCount} fields × {LangCount} languages",
            fields.Count, results.Count);

        return results;
    }

    public async Task<IReadOnlyList<EntityFieldTranslationSet>> TranslateToAllActiveLanguagesAsync(
        IReadOnlyDictionary<string, string> fields,
        string sourceLanguageCode,
        CancellationToken ct = default)
    {
        var activeLanguages = await activeLanguageProvider.GetActiveLanguagesAsync(ct);

        var targetCodes = activeLanguages
            .Where(l => !string.Equals(l.Code, sourceLanguageCode, StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Code)
            .ToList();

        if (targetCodes.Count == 0)
        {
            logger.LogDebug("No active target languages found (source: {Source})", sourceLanguageCode);
            return [];
        }

        return await TranslateAsync(fields, sourceLanguageCode, targetCodes, ct);
    }
}
