using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class BlogComment : AuditableEntity, IAggregateRoot
{
    /// <summary>Replacement content used when a comment is soft-deleted.</summary>
    public const string RedactedContentMarker = "[deleted]";

    /// <summary>Maximum allowed content length (after trim).</summary>
    public const int MaxContentLength = 1000;

    /// <summary>
    /// Maximum nesting depth for replies.
    /// 0 = root, 1 = reply to root, 2 = reply to a reply. Anything deeper is rejected.
    /// </summary>
    public const int MaxReplyDepth = 2;

    private readonly List<BlogComment> _replies = [];
    private readonly List<BlogCommentReaction> _reactions = [];

    private BlogComment() { } // EF Core

    public Guid BlogId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int LikeCount { get; private set; } = 0;

    /// <summary>
    /// <c>true</c> when the comment has been soft-deleted: <see cref="Content"/> is
    /// replaced with the redaction marker but the row and its replies remain visible
    /// so the thread structure is preserved. We deliberately do NOT use the inherited
    /// <c>IsDeleted</c> flag for this state because the EF query filter excludes
    /// rows where <c>IsDeleted == true</c>, which would hide the redacted comment
    /// and break thread visibility.
    /// </summary>
    public bool IsContentRedacted { get; private set; }

    public Blog Blog { get; private set; } = default!;
    public BlogComment? ParentComment { get; private set; }
    public IReadOnlyCollection<BlogComment> Replies => _replies.AsReadOnly();
    public IReadOnlyCollection<BlogCommentReaction> Reactions => _reactions.AsReadOnly();

    /// <summary>
    /// Factory for a new <see cref="BlogComment"/>. The blog's status must be
    /// <see cref="BlogStatus.Published"/>. When <paramref name="parent"/> is supplied,
    /// the caller MUST have eagerly loaded <c>parent.ParentComment</c> so the depth
    /// invariant can be validated; otherwise the call is rejected.
    /// </summary>
    public static BlogComment Create(
        Guid blogId,
        Guid userId,
        string content,
        BlogComment? parent,
        BlogStatus blogStatus,
        DateTime utcNow)
    {
        if (blogId == Guid.Empty)
            throw new ArgumentException("BlogId is required.", nameof(blogId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (blogStatus != BlogStatus.Published)
        {
            throw new InvalidOperationException(
                $"BlogComment.BlogNotPublished: comments are only allowed on Published blogs. " +
                $"Current status: {blogStatus}.");
        }

        ValidateContent(content);

        Guid? parentCommentId = null;
        if (parent is not null)
        {
            if (parent.BlogId != blogId)
            {
                throw new InvalidOperationException(
                    "BlogComment.ParentBlogMismatch: parent comment belongs to a different blog.");
            }

            // Depth of the new comment = parent.depth + 1.
            // Allowed parent depths: 0 (root) or 1 (reply-to-root). Deeper => reject.
            var parentDepth = ComputeDepthFromLoadedChain(parent);
            if (parentDepth + 1 > MaxReplyDepth)
            {
                throw new InvalidOperationException(
                    $"BlogComment.MaxDepthExceeded: nested replies are limited to depth {MaxReplyDepth}.");
            }

            parentCommentId = parent.Id;
        }

        var comment = new BlogComment
        {
            BlogId = blogId,
            UserId = userId,
            ParentCommentId = parentCommentId,
            Content = content.Trim(),
            LikeCount = 0,
            IsContentRedacted = false,
        };
        comment.CreatedAt = utcNow;

        comment.AddDomainEvent(new BlogCommentCreatedDomainEvent(
            CommentId: comment.Id,
            BlogId: comment.BlogId,
            UserId: comment.UserId,
            ParentCommentId: comment.ParentCommentId,
            CreatedAtUtc: utcNow));

        return comment;
    }

    /// <summary>
    /// Edits the comment's content. Rejected if the comment has already been redacted.
    /// </summary>
    public void Edit(string newContent, DateTime utcNow)
    {
        if (IsContentRedacted)
        {
            throw new InvalidOperationException(
                "BlogComment.Redacted: redacted comments cannot be edited.");
        }

        ValidateContent(newContent);

        var trimmed = newContent.Trim();
        if (string.Equals(Content, trimmed, StringComparison.Ordinal))
        {
            // No-op: avoid spurious UpdatedAt bump + domain event.
            return;
        }

        Content = trimmed;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogCommentUpdatedDomainEvent(
            CommentId: Id,
            BlogId: BlogId,
            UserId: UserId,
            UpdatedAtUtc: utcNow));
    }

    /// <summary>
    /// Soft-deletes the comment by redacting its content. Idempotent: a second call
    /// is a no-op and does NOT emit another domain event. The row and its replies
    /// stay visible so the thread is preserved.
    /// </summary>
    public void Redact(DateTime utcNow)
    {
        if (IsContentRedacted)
            return;

        Content = RedactedContentMarker;
        IsContentRedacted = true;
        UpdatedAt = utcNow;

        AddDomainEvent(new BlogCommentDeletedDomainEvent(
            CommentId: Id,
            BlogId: BlogId,
            UserId: UserId,
            DeletedAtUtc: utcNow));
    }

    private static int ComputeDepthFromLoadedChain(BlogComment node)
    {
        // Walks the already-loaded ParentComment chain. Caller is responsible for
        // including ParentComment (and its ParentComment) so the chain is complete
        // up to MaxReplyDepth. If the chain is shorter than depth, that means the
        // node is at or near root.
        var depth = 0;
        var cursor = node;
        // Safety bound: walk at most MaxReplyDepth + 1 levels.
        for (var i = 0; i <= MaxReplyDepth + 1; i++)
        {
            if (cursor.ParentCommentId is null)
                return depth;
            if (cursor.ParentComment is null)
            {
                // Navigation not loaded → fail-closed: assume max depth was reached
                // so callers are forced to load the chain.
                throw new InvalidOperationException(
                    "BlogComment.ParentChainNotLoaded: parent navigation must be eagerly loaded " +
                    "to validate nesting depth.");
            }

            depth++;
            cursor = cursor.ParentComment;
        }

        return depth;
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content is required.", nameof(content));

        if (content.Trim().Length > MaxContentLength)
        {
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxContentLength} characters.",
                nameof(content));
        }
    }
}
