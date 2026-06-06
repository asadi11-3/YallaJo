using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Application.Queries.AccessibilityReview.Common;

/// <summary>Read-model for <see cref="AccessibilityReview"/>.</summary>
public sealed record AccessibilityReviewDto(
    Guid Id,
    Guid UserId,
    ReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv,
    AccessibilityReviewStatus Status,
    DateTime CreatedAt,
    DateTime? LastEditedAt)
{
    public static AccessibilityReviewDto From(Social.Domain.Entities.AccessibilityReview review) => new(
        review.Id,
        review.UserId,
        review.TargetType,
        review.TargetId,
        review.Rating,
        review.Title,
        review.Content,
        review.VisitDate,
        review.FeatureTypesCsv,
        review.Status,
        review.CreatedAt,
        review.LastEditedAt);
}

public sealed record AccessibilityReviewPageDto(
    IReadOnlyList<AccessibilityReviewDto> Items,
    Guid? NextCursor);

public sealed record PublicAccessibilityReviewPageDto(
    IReadOnlyList<AccessibilityReviewDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
