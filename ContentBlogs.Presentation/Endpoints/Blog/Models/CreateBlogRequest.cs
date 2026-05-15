namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record CreateBlogRequest(
    string Title,
    string Content,
    string SourceLanguageCode,
    string? Slug = null,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null);
