namespace ContentBlogs.Application.Queries.Blog.Dtos;

public sealed record AdminBlogDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Content,
    string? Summary,
    string Status,
    DateTime? PublishedAt,
    int ViewCount,
    int? ReadTimeMinutes,
    string? MetaTitle,
    string? MetaDescription,
    Guid? PlaceId,
    string LanguageCode,
    byte[] RowVersion,
    int TourCount,
    IReadOnlyCollection<BlogTourSummaryDto> LinkedTours,
    bool IsFeatured);
