namespace YallaJo.Web.Areas.Public.Models.AccessibilityReviews;

public static class AccessibilityReviewMapper
{
    /// <summary>Canonical feature-kind vocabulary for the checklist (mirrors the
    /// AccessibilityFeatureType enum). Order drives the checkbox render order.</summary>
    public static readonly IReadOnlyList<string> FeatureKinds =
        Enum.GetNames<AccessibilityFeatureKind>();

    public static AccessibilityReviewRowVm ToRow(AccessibilityReviewResponse r) => new(
        r.Id,
        r.UserId,
        AuthorHandle(r.UserId),
        r.Rating,
        r.Title,
        r.Content,
        r.VisitDate,
        ParseFeatures(r.FeatureTypesCsv),
        r.CreatedAt,
        r.LastEditedAt);

    /// <summary>Privacy-safe author handle — accessibility reviews can reveal sensitive
    /// needs, and the DTO carries no name/email, so never surface the raw GUID.</summary>
    public static string AuthorHandle(Guid userId) => $"Reviewer {userId.ToString("N")[..6]}";

    public static IReadOnlyList<string> ParseFeatures(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .ToList();

    /// <summary>Normalizes selected checkbox values to a clean CSV of known kinds.</summary>
    public static string ToCsv(IEnumerable<string> selected)
    {
        var known = selected
            .Where(s => Enum.TryParse<AccessibilityFeatureKind>(s, ignoreCase: true, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return string.Join(',', known);
    }

    // ── TargetType string <-> enum (the API binds the enum by name) ────────────

    public static string TargetTypeName(AccessibilityReviewTargetType t) => t.ToString();

    public static AccessibilityReviewTargetType ParseTargetType(string targetType) =>
        Enum.TryParse<AccessibilityReviewTargetType>(targetType, ignoreCase: true, out var t)
            ? t
            : AccessibilityReviewTargetType.Place;
}
