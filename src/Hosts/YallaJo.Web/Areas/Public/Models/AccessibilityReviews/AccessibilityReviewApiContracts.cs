namespace YallaJo.Web.Areas.Public.Models.AccessibilityReviews;

// Contracts for /api/v1/social/accessibility/reviews. These are DISTINCT from the
// normal-review contracts (Models/Reviews) — accessibility reviews carry a
// FeatureTypesCsv and NO RowVersion, and DELETE is bodyless.

/// <summary>Mirrors Social ReviewTargetType (Tour=0, Place=1, Business=2, TourGuide=3).</summary>
public enum AccessibilityReviewTargetType : byte
{
    Tour = 0,
    Place = 1,
    Business = 2,
    TourGuide = 3,
}

/// <summary>Mirrors Social AccessibilityReviewStatus.</summary>
public enum AccessibilityReviewStatus : byte
{
    Visible = 0,
    Hidden = 1,
    Removed = 2,
}

/// <summary>Controlled vocabulary for the FeatureTypesCsv checklist. Mirrors the
/// ContentPlaces/Business AccessibilityFeatureType enum (verified suitable for reviews).</summary>
public enum AccessibilityFeatureKind : byte
{
    Wheelchair = 0,
    Visual = 1,
    Hearing = 2,
    Cognitive = 3,
    Mobility = 4,
    Other = 5,
}

/// <summary>Mirrors Social AccessibilityReviewDto.</summary>
public sealed record AccessibilityReviewResponse(
    Guid Id,
    Guid UserId,
    AccessibilityReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv,
    AccessibilityReviewStatus Status,
    DateTime CreatedAt,
    DateTime? LastEditedAt);

/// <summary>Mirrors Social PublicAccessibilityReviewPageDto.</summary>
public sealed record PublicAccessibilityReviewPageResponse(
    IReadOnlyList<AccessibilityReviewResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>Mirrors Social AccessibilityReviewPageDto (cursor-paginated "my" list).</summary>
public sealed record MyAccessibilityReviewPageResponse(
    IReadOnlyList<AccessibilityReviewResponse> Items,
    Guid? NextCursor);

// ── Request bodies (no RowVersion) ─────────────────────────────────────────────

public sealed record CreateAccessibilityReviewBody(
    AccessibilityReviewTargetType TargetType,
    Guid TargetId,
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv);

public sealed record UpdateAccessibilityReviewBody(
    decimal Rating,
    string? Title,
    string Content,
    DateOnly? VisitDate,
    string FeatureTypesCsv);
