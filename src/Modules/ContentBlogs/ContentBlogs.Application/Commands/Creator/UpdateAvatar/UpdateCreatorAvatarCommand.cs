using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UpdateAvatar;

public sealed record UpdateCreatorAvatarCommand(string AvatarUrl) : ICommand;
