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
}
