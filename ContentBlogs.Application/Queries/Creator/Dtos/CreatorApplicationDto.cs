using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorApplicationDto(
    Guid Id,
    Guid ApplicantUserId,
    string? Bio,
    CreatorApplicationStatus Status,
    CreatorApplicationSource Source,
    IReadOnlyList<string> PortfolioUrls,
    IReadOnlyList<string> SampleWorkUrls,
    IReadOnlyList<Guid> NicheIds,
    IReadOnlyList<string> FreeTags,
    IReadOnlyList<Guid> LanguageIds,
    IReadOnlyList<Guid> PreferredRegionIds,
    IReadOnlyDictionary<string, string> SocialHandles,
    string? AdminNote,
    int ReapplicationCount,
    DateTime? LastRejectedAt,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
