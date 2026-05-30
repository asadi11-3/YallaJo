using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorProfileDto(
    Guid Id,
    Guid UserId,
    string Slug,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    CreatorTrustTier TrustTier,
    CreatorProfileStatus Status,
    int ArticleCount,
    long TotalViewCount,
    long TotalReactionCount,
    long TotalCommentCount,
    int FollowerCount,
    Guid? LinkedProviderId,
    DateTime CreatedAt);
