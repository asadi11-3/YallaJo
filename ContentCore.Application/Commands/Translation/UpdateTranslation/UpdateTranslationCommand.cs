using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed record UpdateTranslationResult(Guid Id, string TranslatedText, string Status);

public sealed record UpdateTranslationCommand(
    Guid Id,
    string TranslatedText) : ICommand<UpdateTranslationResult>;
