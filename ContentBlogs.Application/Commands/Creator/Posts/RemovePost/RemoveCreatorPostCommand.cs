using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.RemovePost;

/// <summary>
/// Admin removes a published post (moderation action).
/// </summary>
public sealed record RemoveCreatorPostCommand(Guid PostId, string Reason) : ICommand;
