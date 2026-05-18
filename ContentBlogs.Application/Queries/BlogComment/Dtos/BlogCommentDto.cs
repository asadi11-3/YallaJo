namespace ContentBlogs.Application.Queries.BlogComment.Dtos;

public sealed record BlogCommentDto(
    Guid Id,
    Guid BlogId,
    Guid? ParentCommentId,
    Guid UserId,
    string Content,
    bool IsContentRedacted,
    int LikeCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
