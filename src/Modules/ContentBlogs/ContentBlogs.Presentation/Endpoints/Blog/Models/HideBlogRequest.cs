namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record HideBlogRequest(byte[] RowVersion, string Reason);
