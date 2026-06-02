namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class BlogCommentVm
{
    public Guid Id { get; init; }
    public Guid? ParentCommentId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsRedacted { get; init; }
    public int LikeCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public bool IsReply => ParentCommentId.HasValue;
}
