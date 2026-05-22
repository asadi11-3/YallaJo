using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorPostSummaryDto(
    Guid Id,
    Guid CreatorProfileId,
    string CreatorDisplayName,
    string CreatorSlug,
    CreatorPostType PostType,
    string Slug,
    string Title,
    string Excerpt,
    CreatorPostStatus Status,
    DateTime? PublishedAt,
    bool IsFeatured,
    bool IsSponsored,
    long ViewCount,
    long ReactionCount,
    long CommentCount,
    DateTime CreatedAt);
