using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed record BatchTranslateResultItem(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);

public sealed record BatchTranslateResult(IReadOnlyList<BatchTranslateResultItem> Translations);

public sealed record BatchTranslateCommand(
    IReadOnlyList<string> Texts,
    string FromLanguageCode,
    string ToLanguageCode) : ICommand<BatchTranslateResult>;
