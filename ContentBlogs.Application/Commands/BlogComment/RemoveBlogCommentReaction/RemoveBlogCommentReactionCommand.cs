using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.BlogComment.RemoveBlogCommentReaction;

/// <summary>
/// Removes the current user's reaction from a comment. Idempotent: succeeds
/// even when no reaction exists for the caller.
/// </summary>
public sealed record RemoveBlogCommentReactionCommand(
    Guid CommentId) : ICommand;
