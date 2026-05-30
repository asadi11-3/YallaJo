using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed record UpdateTranslationCommand(
    Guid Id,
    string TranslatedText) : ICommand<UpdateTranslationResult>;
