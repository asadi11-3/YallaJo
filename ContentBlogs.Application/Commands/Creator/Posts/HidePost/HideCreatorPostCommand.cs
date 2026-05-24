using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.HidePost;

/// <summary>
/// Admin hides a published post (moderation action).
/// </summary>
public sealed record HideCreatorPostCommand(Guid PostId, string Reason) : ICommand;
