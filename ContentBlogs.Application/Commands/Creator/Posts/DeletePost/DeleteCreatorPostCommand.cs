using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.DeletePost;

/// <summary>
/// Deletes a draft or rejected post. Only the creator who owns it may delete.
/// </summary>
public sealed record DeleteCreatorPostCommand(Guid PostId) : ICommand;
