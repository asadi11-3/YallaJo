using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed record BatchTranslateCommand(
    IReadOnlyList<string> Texts,
    string FromLanguageCode,
    string ToLanguageCode) : ICommand<BatchTranslateResult>;
