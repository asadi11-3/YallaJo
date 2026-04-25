using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.DeleteAvatar;

public sealed record DeleteAvatarCommand() : ICommand<DeleteAvatarResult>;
