using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.FollowCreator;

public sealed record FollowCreatorCommand(Guid CreatorProfileId) : ICommand;
