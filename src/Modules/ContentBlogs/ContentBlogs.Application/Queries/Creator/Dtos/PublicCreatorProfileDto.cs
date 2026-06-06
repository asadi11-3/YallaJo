using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

/// <summary>
/// Public-safe projection of a creator profile for the anonymous by-slug endpoint
/// (Creator Backend Contract Polish, Gap 4).
/// <para>
/// Deliberately EXCLUDES internal/private fields that <see cref="CreatorProfileDto"/>
/// carries: <c>UserId</c>, <c>Status</c>, <c>LinkedProviderId</c>, and <c>CreatedAt</c>.
/// Only an Active profile is ever exposed through this DTO, so a status field would be
/// redundant and a privacy leak.
/// </para>
/// </summary>
public sealed record PublicCreatorProfileDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    CreatorTrustTier TrustTier,
    int ArticleCount,
    long TotalViewCount,
    long TotalReactionCount,
    long TotalCommentCount,
    int FollowerCount);
