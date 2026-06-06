using Social.Domain.Enums;
using Social.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>
/// Accessibility-focused review for a tour, place, business, or tour-guide (Phase-3 WS-2, G1).
///
/// Independent from <see cref="Review"/>: travelers describe how accessible the venue/experience
/// was, optionally tagging which accessibility features they used (Wheelchair/Visual/Hearing/
/// Cognitive/Mobility/Other). Reuses <see cref="ReviewTargetType"/>.
///
/// Rules (S-AR* mirrors S-R* on the Review aggregate):
///   S-AR1: One per (UserId, TargetType, TargetId) WHERE IsDeleted=0 — unique index in DB.
///   S-AR2: 48-hour edit window from CreatedAt (same as Review).
///   S-AR3: Rating in 0.5 increments from 1.0 to 5.0.
///   S-AR4: FeatureTypesCsv is a comma-separated list of <see cref="ContentPlaces.Domain.Enums.AccessibilityFeatureType"/>
///          values stored as strings; consumers may parse and re-enum on read.
///          No cross-module FK or enum dependency in this aggregate (intentional decoupling).
///   S-AR5: MVP — no profanity gate, no auto-hide, no admin moderation queue (deferred to Phase 3.5).
/// </summary>
public sealed class AccessibilityReview : AuditableEntity, IAggregateRoot
{
    private AccessibilityReview() { } // EF Core

    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid UserId                  { get; private set; }
    public ReviewTargetType TargetType  { get; private set; }
    public Guid TargetId                { get; private set; }

    // ── Content ──────────────────────────────────────────────────────────────
    /// <summary>Rating in 0.5 increments from 1.0 to 5.0 (S-AR3).</summary>
    public decimal Rating               { get; private set; }
    public string? Title                { get; private set; }
    public string Content               { get; private set; } = string.Empty;
    public DateOnly? VisitDate          { get; private set; }

    /// <summary>
    /// Comma-separated list of accessibility-feature-type names the reviewer used / experienced.
    /// Stored as strings (not enum FK) to keep Social decoupled from ContentPlaces (S-AR4).
    /// Example: "Wheelchair,Visual,Mobility".
    /// </summary>
    public string FeatureTypesCsv       { get; private set; } = string.Empty;

    // ── Status ───────────────────────────────────────────────────────────────
    public AccessibilityReviewStatus Status { get; private set; } = AccessibilityReviewStatus.Published;
    public DateTime? LastEditedAt           { get; private set; }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static AccessibilityReview Create(
        Guid userId,
        ReviewTargetType targetType,
        Guid targetId,
        decimal rating,
        string? title,
        string content,
        DateOnly? visitDate,
        string featureTypesCsv,
        TimeProvider timeProvider)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId must be non-empty.", nameof(userId));
        if (targetId == Guid.Empty)
            throw new ArgumentException("TargetId must be non-empty.", nameof(targetId));
        ValidateRating(rating);

        var normalisedCsv = NormaliseFeatureTypesCsv(featureTypesCsv);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var review = new AccessibilityReview
        {
            UserId          = userId,
            TargetType      = targetType,
            TargetId        = targetId,
            Rating          = rating,
            Title           = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Content         = content,
            VisitDate       = visitDate,
            FeatureTypesCsv = normalisedCsv,
            Status          = AccessibilityReviewStatus.Published,
        };

        review.AddDomainEvent(new AccessibilityReviewSubmittedDomainEvent(
            review.Id, userId, targetType, targetId, rating, normalisedCsv, now));

        return review;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public void Edit(
        decimal newRating,
        string? newTitle,
        string newContent,
        DateOnly? newVisitDate,
        string newFeatureTypesCsv,
        TimeProvider timeProvider)
    {
        if (Status is AccessibilityReviewStatus.DeletedByUser or AccessibilityReviewStatus.RemovedByAdmin)
            throw new InvalidOperationException("Cannot edit a deleted accessibility review.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if ((now - CreatedAt).TotalHours > 48)
            throw new InvalidOperationException("AccessibilityReview.EditWindowExpired");

        ValidateRating(newRating);

        Rating          = newRating;
        Title           = string.IsNullOrWhiteSpace(newTitle) ? null : newTitle.Trim();
        Content         = newContent;
        VisitDate       = newVisitDate;
        FeatureTypesCsv = NormaliseFeatureTypesCsv(newFeatureTypesCsv);
        LastEditedAt    = now;
        MarkUpdated();
    }

    public void Delete(bool isAdmin)
    {
        if (Status is AccessibilityReviewStatus.DeletedByUser or AccessibilityReviewStatus.RemovedByAdmin)
            return; // idempotent

        Status = isAdmin
            ? AccessibilityReviewStatus.RemovedByAdmin
            : AccessibilityReviewStatus.DeletedByUser;
        SoftDelete();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    public static bool IsHalfStarRating(decimal rating)
        => rating >= 0.5m && rating <= 5.0m && (rating * 2) % 1 == 0;

    private static void ValidateRating(decimal rating)
    {
        if (!IsHalfStarRating(rating))
            throw new ArgumentOutOfRangeException(nameof(rating),
                $"Rating must be 0.5-5.0 in 0.5 increments, got {rating}.");
    }

    /// <summary>
    /// Normalise the comma-separated feature-type list: trim, drop blanks, dedupe (case-insensitive),
    /// preserve original casing of the first occurrence, join with comma (no spaces). Whitelist NOT
    /// enforced at the domain level (intentional — see S-AR4); callers/validators can enforce a known
    /// set of values from <see cref="ContentPlaces.Domain.Enums.AccessibilityFeatureType"/>.
    /// </summary>
    private static string NormaliseFeatureTypesCsv(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (seen.Add(part))
                kept.Add(part);
        }
        return string.Join(',', kept);
    }
}
