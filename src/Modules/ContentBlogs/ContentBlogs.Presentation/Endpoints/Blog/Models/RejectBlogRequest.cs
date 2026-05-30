namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record RejectBlogRequest(byte[] RowVersion, string Reason);
