namespace YallaJo.Web.Areas.Guide.Models.Reviews;

/// <summary>Single review item as returned by the public reviews endpoint.</summary>
public sealed record ReviewItemResponse(
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
    int HelpfulVoteCount,
    DateTime? LastEditedAt,
    DateTime CreatedAt,
    IReadOnlyList<ReviewReplyResponse> Replies);

public sealed record ReviewReplyResponse(
    Guid Id,
    Guid ReviewId,
    Guid ProviderUserId,
    string Content,
    DateTime CreatedAt,
    DateTime? LastEditedAt);

/// <summary>Paged public-review envelope (matches Social.PublicReviewPageDto JSON).</summary>
public sealed record PublicReviewPageResponse(
    IReadOnlyList<ReviewItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Body for POST /api/v1/social/reviews/{id}/reply (B3 — guide replies to a review).</summary>
public sealed record AddReviewReplyRequest(string Content);

/// <summary>Aggregate rating summary (matches Social.RatingSummaryDto JSON).</summary>
public sealed record RatingSummaryResponse(
    string EntityType,
    Guid EntityId,
    decimal AverageRating,
    int ReviewCount);
