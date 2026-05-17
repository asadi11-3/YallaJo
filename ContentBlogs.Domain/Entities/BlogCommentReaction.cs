using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class BlogCommentReaction : BaseEntity
{
    private BlogCommentReaction() { } // EF Core

    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }
    public ReactionType ReactionType { get; private set; }

    public BlogComment BlogComment { get; private set; } = default!;

    // Creates a reaction owned and managed by the parent <see cref="BlogComment"/>.

    internal static BlogCommentReaction Create(
        Guid commentId,
        Guid userId,
        ReactionType reactionType,
        DateTime utcNow)
    {
        if (commentId == Guid.Empty)
            throw new ArgumentException("CommentId is required.", nameof(commentId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var reaction = new BlogCommentReaction
        {
            CommentId = commentId,
            UserId = userId,
            ReactionType = reactionType,
        };
        reaction.CreatedAt = utcNow;
        return reaction;
    }

    // Updates the user's existing reaction when the reaction type changes.
    internal void ChangeType(ReactionType newType, DateTime utcNow)
    {
        if (ReactionType == newType)
            return;

        ReactionType = newType;
        UpdatedAt = utcNow;
    }
}
