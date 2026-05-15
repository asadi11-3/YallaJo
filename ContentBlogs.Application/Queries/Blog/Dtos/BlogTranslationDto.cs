namespace ContentBlogs.Application.Queries.Blog.Dtos;


public sealed record BlogTranslationDto(
    string LanguageCode,
    string Title,
    string Content,
    string? Summary);
