namespace ContentBlogs.Presentation.Endpoints.BlogComment.Models;

public sealed record CreateBlogCommentRequest(
    string Content,
    Guid? ParentCommentId = null);
