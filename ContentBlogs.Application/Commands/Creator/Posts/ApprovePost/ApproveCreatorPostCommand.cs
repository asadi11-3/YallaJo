using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.ApprovePost;

public sealed record ApproveCreatorPostCommand(Guid PostId) : ICommand;
