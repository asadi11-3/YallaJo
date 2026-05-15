namespace ContentBlogs.Application.Queries.Blog.Dtos;


public sealed record BlogSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    DateTime? PublishedAt,
    int ViewCount,
    int? ReadTimeMinutes,
    Guid? PlaceId,
    string LanguageCode);
