using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.DeleteLanguage;

public sealed record DeleteLanguageCommand(Guid Id) : ICommand;
