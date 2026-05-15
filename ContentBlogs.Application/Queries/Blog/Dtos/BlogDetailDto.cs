namespace ContentBlogs.Application.Queries.Blog.Dtos;

public sealed record BlogDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Content,
    string? Summary,
    DateTime? PublishedAt,
    int ViewCount,
    int? ReadTimeMinutes,
    string? MetaTitle,
    string? MetaDescription,
    Guid? PlaceId,
    string LanguageCode);
