namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed record TranslateTextResult(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);
