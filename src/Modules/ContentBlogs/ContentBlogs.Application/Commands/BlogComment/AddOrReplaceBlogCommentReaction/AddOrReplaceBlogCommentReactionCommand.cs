using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.BlogComment.AddOrReplaceBlogCommentReaction;

/// <summary>
/// Adds the current user's reaction to a comment, or replaces it with a
/// different <see cref="ReactionType"/> if one already exists. Idempotent
/// when the supplied type matches the existing reaction.
/// </summary>
public sealed record AddOrReplaceBlogCommentReactionCommand(
    Guid CommentId,
    ReactionType ReactionType) : ICommand;
