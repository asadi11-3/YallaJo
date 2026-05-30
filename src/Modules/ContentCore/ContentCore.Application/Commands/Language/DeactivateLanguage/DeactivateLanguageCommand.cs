using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.DeactivateLanguage;

public sealed record DeactivateLanguageCommand(Guid Id) : ICommand;
