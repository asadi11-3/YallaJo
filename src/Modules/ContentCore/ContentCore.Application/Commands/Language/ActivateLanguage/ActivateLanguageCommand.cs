using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.ActivateLanguage;

public sealed record ActivateLanguageCommand(Guid Id) : ICommand;
