using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed record TranslateTextResult(
    string OriginalText,
    string TranslatedText,
    string FromLanguage,
    string ToLanguage,
    double? Confidence);

public sealed record TranslateTextCommand(
    string Text,
    string FromLanguageCode,
    string ToLanguageCode) : ICommand<TranslateTextResult>;
