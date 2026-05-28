namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record RemoveBlogRequest(byte[] RowVersion, string Reason);
