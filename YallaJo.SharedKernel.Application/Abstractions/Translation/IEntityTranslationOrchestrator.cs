namespace YallaJo.SharedKernel.Application.Abstractions.Translation;

/// <summary>
/// High-level orchestrator that batch-translates a set of entity fields
/// to one or more target languages using the configured <see cref="ITranslationService"/>.
///
/// Usage in a command handler:
/// <code>
/// var fields = new Dictionary&lt;string, string&gt;
/// {
///     ["Name"] = place.Name,
///     ["Description"] = place.Description ?? ""
/// };
///
/// var sets = await _orchestrator.TranslateToAllActiveLanguagesAsync(fields, "en", ct);
///
/// foreach (var set in sets)
/// {
///     var translation = PlaceTranslation.Create(
///         place.Id, set.LanguageId,
///         set.Fields["Name"],
///         set.Fields["Description"]);
///     await _repo.AddAsync(translation, ct);
/// }
/// </code>
/// </summary>
public interface IEntityTranslationOrchestrator
{
    /// <summary>
    /// Translate entity fields to specific target languages.
    /// </summary>
    Task<IReadOnlyList<EntityFieldTranslationSet>> TranslateAsync(
        IReadOnlyDictionary<string, string> fields,
        string sourceLanguageCode,
        IReadOnlyList<string> targetLanguageCodes,
        CancellationToken ct = default);

    /// <summary>
    /// Translate entity fields to ALL active languages (excluding the source language).
    /// Internally queries <see cref="IActiveLanguageProvider"/> for the active set.
    /// </summary>
    Task<IReadOnlyList<EntityFieldTranslationSet>> TranslateToAllActiveLanguagesAsync(
        IReadOnlyDictionary<string, string> fields,
        string sourceLanguageCode,
        CancellationToken ct = default);
}

/// <summary>
/// A translated field set for a single target language.
/// </summary>
public sealed record EntityFieldTranslationSet(
    Guid LanguageId,
    string LanguageCode,
    IReadOnlyDictionary<string, string> Fields);
