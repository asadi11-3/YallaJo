using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.ValueObjects;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorPostDto(
    Guid Id,
    Guid CreatorProfileId,
    string CreatorDisplayName,
    string CreatorSlug,
    CreatorPostType PostType,
    string Slug,
    string Title,
    string Excerpt,
    string? Body,
    Guid LanguageId,
    CreatorPostStatus Status,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedByAdminId,
    DateTime? PublishedAt,
    string? RejectionReason,
    bool IsFeatured,
    DateTime? FeaturedAt,
    Guid? FeaturedByAdminId,
    DateTime? FeaturedUntil,
    bool IsSponsored,
    IReadOnlyList<DisclosureTarget> DisclosedTargets,
    long ViewCount,
    long ReactionCount,
    long CommentCount,
    int ReportCount,
    List<Guid> NicheIds,
    List<string> FreeTags,
    string? TypeSpecificDataJson,
    DateTime CreatedAt);
