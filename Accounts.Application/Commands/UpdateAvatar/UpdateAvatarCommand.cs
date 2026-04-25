using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.UpdateAvatar;

public sealed record UpdateAvatarCommand(string AvatarUrl) : ICommand<UpdateAvatarResult>;
