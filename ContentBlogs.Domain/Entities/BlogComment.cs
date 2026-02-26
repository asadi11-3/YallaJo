using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class BlogComment : AuditableEntity
{
    private readonly List<BlogComment> _replies = [];
    private readonly List<BlogCommentReaction> _reactions = [];

    private BlogComment() { } // EF Core

    public Guid BlogId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int LikeCount { get; private set; } = 0;

    public Blog Blog { get; private set; } = default!;
    public BlogComment? ParentComment { get; private set; }
    public IReadOnlyCollection<BlogComment> Replies => _replies.AsReadOnly();
    public IReadOnlyCollection<BlogCommentReaction> Reactions => _reactions.AsReadOnly();
}
