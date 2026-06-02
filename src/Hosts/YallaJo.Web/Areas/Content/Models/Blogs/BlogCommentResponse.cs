namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogCommentResponse
{
    public Guid Id { get; init; }
    public Guid BlogId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public Guid UserId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsContentRedacted { get; init; }
    public int LikeCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
