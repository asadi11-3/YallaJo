using Social.Domain.Enums;

namespace Social.Application.Queries.Dtos;

/// <summary>Projection of a Review aggregate for read operations.</summary>
public sealed record ReviewDto(
    Guid Id,
    Guid UserId,
    string TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string Status,
    bool IsVerifiedBooking,
    bool ProfanityFlagged,
    int CurrentReportCount,
    DateTime? LastEditedAt,
    DateTime? AutoHiddenAt,
    DateTime CreatedAt,
    int HelpfulVoteCount,
    IReadOnlyList<ReviewReplyDto> Replies
);

public sealed record ReviewReplyDto(
    Guid Id,
    Guid ReviewId,
    Guid ProviderUserId,
    string Content,
    DateTime CreatedAt,
    DateTime? LastEditedAt
);

public sealed record ReviewPageDto(
    IReadOnlyList<ReviewDto> Items,
    string? NextCursor
);

public sealed record PublicReviewPageDto(
    IReadOnlyList<ReviewDto> Items,
    int Page,
    int PageSize,
    int TotalCount
);

public sealed record RatingSummaryDto(
    string EntityType,
    Guid EntityId,
    decimal AverageRating,
    int ReviewCount
);
