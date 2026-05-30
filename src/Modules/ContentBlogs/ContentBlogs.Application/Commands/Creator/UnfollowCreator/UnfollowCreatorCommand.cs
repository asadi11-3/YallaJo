using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UnfollowCreator;

public sealed record UnfollowCreatorCommand(Guid CreatorProfileId) : ICommand;
