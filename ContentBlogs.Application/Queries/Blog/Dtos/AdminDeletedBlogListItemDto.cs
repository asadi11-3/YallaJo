namespace ContentBlogs.Application.Queries.Blog.Dtos;

public sealed record AdminDeletedBlogListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Status,
    bool IsFeatured,
    Guid? PlaceId,
    DateTime? PublishedAt,
    DateTime? DeletedAt,
    DateTime? UpdatedAt,
    byte[] RowVersion);
