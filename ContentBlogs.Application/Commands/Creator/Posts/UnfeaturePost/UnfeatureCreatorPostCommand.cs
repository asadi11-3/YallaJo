using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.UnfeaturePost;

public sealed record UnfeatureCreatorPostCommand(Guid PostId) : ICommand;
