using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.UnhidePost;

/// <summary>
/// Admin unhides a hidden post, restoring it to Published status.
/// </summary>
public sealed record UnhideCreatorPostCommand(Guid PostId) : ICommand;
