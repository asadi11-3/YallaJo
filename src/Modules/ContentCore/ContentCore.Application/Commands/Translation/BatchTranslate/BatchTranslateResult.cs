namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed record BatchTranslateResultItem(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);

public sealed record BatchTranslateResult(IReadOnlyList<BatchTranslateResultItem> Translations);
