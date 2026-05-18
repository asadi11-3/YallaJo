namespace ContentBlogs.Presentation.Endpoints.BlogComment.Models;

public sealed record UpdateBlogCommentRequest(byte[] RowVersion, string Content);
