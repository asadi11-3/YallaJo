namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record UpdateBlogRequest(
    string Title,
    string Slug,
    string Content,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null,
    int? ReadTimeMinutes = null);
