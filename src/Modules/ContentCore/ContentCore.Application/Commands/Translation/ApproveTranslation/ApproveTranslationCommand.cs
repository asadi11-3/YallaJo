using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.ApproveTranslation;

public sealed record ApproveTranslationCommand(Guid Id) : ICommand;
