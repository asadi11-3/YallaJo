namespace YallaJo.SharedKernel.Application.Abstractions.Translation;

/// <summary>
/// Abstraction for translating text via an external translation API.
/// Implementations may target Azure Translator, Google Cloud Translation, DeepL, etc.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate a single text from source language to target language.
    /// </summary>
    Task<TranslationResult> TranslateAsync(
        string text,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Translate multiple texts in a single API call (batch optimization).
    /// </summary>
    Task<IReadOnlyList<TranslationResult>> BatchTranslateAsync(
        IReadOnlyList<string> texts,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Detect the language of a given text.
    /// </summary>
    Task<string> DetectLanguageAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Get all supported language codes from the provider.
    /// </summary>
    Task<IReadOnlyList<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken ct = default);
}

public sealed record TranslationResult(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);

public sealed record SupportedLanguage(string Code, string Name, string NativeName);
