namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorProfileSummaryDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string? AvatarUrl,
    int ArticleCount,
    int FollowerCount);
