using Microsoft.EntityFrameworkCore;
using Social.Domain.Entities;
using Social.Domain.Enums;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace Social.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only FE-2D smoke seeder for <see cref="AccessibilityReview"/>.
///
/// Seeds enough deterministic, clearly-labelled ("FE2D Accessibility Review …") data to visually
/// verify every accessibility-review UI state across all four target types (Place, Tour, Business,
/// TourGuide): ownership, the 48-hour edit window, content variations (ratings / feature combos /
/// optional title &amp; visit date / min &amp; long content), list vs empty-list, and status visibility.
///
/// Conventions / safety:
///   • Invoked only from <c>UseDataSeedingAsync</c>, which is gated to Development (or explicit
///     <c>Seeding:Enabled</c>) — this seeder adds NO production data and contains NO real PII.
///   • Idempotent: dedupes on the unique key (UserId, TargetType, TargetId) — the same key as the
///     DB unique filtered index <c>IX_AccessibilityReviews_User_Target_Unique</c> — so re-running
///     never inserts duplicate rows.
///   • Reflection-based construction (matching <see cref="SocialDbInitializer"/>) bypasses the
///     <see cref="AccessibilityReview.Create"/> factory so it can set a deterministic CreatedAt
///     (to exercise the edit window) and Status, and avoid raising domain events.
///   • Runs at Order 96 — after the target rows (Places/Business 60, Social 70, ContentTours 80,
///     Booking 90, FE2D Smoke Guide 95) so all targets are reachable, though AccessibilityReview
///     has no cross-module FK (S-AR4) so ordering is not strictly required.
///
/// Tester accounts (passwords come from the identity seeder):
///   • userA@yallajo.test  (TestPass!23) — the "logged-in me" who OWNS editable + expired reviews.
///   • userB@yallajo.test  (TestPass!23) — owns NO accessibility reviews (empty My-Reviews state).
///   • traveler.1..4@yallajo.local (P@ssw0rd!) — "other users" (view-only for the tester).
/// </summary>
public sealed class AccessibilityReviewSeeder(SocialDbContext dbContext) : IModuleDbInitializer
{
    // ── Tester accounts ────────────────────────────────────────────────────────
    private static readonly Guid UserMe        = Guid.Parse("b0000000-0000-0000-0000-000000000002"); // userA@yallajo.test
    private static readonly Guid TravelerOne   = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TravelerTwo   = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid TravelerThree = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid TravelerFour  = Guid.Parse("99999999-9999-9999-9999-999999999999");

    // ── Targets (reuse existing public seed entities; FE2D Smoke Guide is seeded separately) ──
    private static readonly Guid PlacePetra        = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid TourPetraExplorer = Guid.Parse("ffffffff-0000-0000-0000-000000000001");
    private static readonly Guid BusinessPetra     = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid SmokeTourGuide    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000a1"); // FE2D Smoke Guide aggregate id
    private static readonly Guid SmokePlaceFull    = Guid.Parse("fe2d0000-0000-0000-0000-0000000000b1"); // FE2D Smoke Place Full (Fe2dSmokePlaceSeeder)

    // NB: PlaceJerash (bbbbbbbb-…-0002) and FE2D Smoke Place Minimal/Empty are intentionally LEFT
    // WITHOUT accessibility reviews to exercise the entity-with-zero-reviews empty-list UI state.

    public int Order => 96;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.AccessibilityReviews
            .IgnoreQueryFilters()
            .Select(r => new { r.UserId, r.TargetType, r.TargetId })
            .ToListAsync(cancellationToken);

        var existingKeys = new HashSet<(Guid, ReviewTargetType, Guid)>(
            existing.Select(x => (x.UserId, x.TargetType, x.TargetId)));

        var newRows = BuildSeedSpecs()
            .Where(s => existingKeys.Add((s.UserId, s.TargetType, s.TargetId))) // append-only, respects unique index
            .Select(Materialise)
            .ToList();

        if (newRows.Count == 0)
            return;

        dbContext.AccessibilityReviews.AddRange(newRows);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<SeedSpec> BuildSeedSpecs()
    {
        var now = DateTime.UtcNow;
        DateTime HoursAgo(double h) => now.AddHours(-h);
        DateOnly DaysAgo(int d) => DateOnly.FromDateTime(now.AddDays(-d));

        // Long-but-valid content (well under the 5000-char backend limit).
        const string longContent =
            "FE2D Accessibility Review (long content). Step-free entrance via a gentle ramp on the north side. " +
            "Wide automatic doors, level flooring throughout the main hall, and a clearly signed accessible restroom " +
            "with grab rails and an emergency cord. Staff offered assistance proactively and knew the step-free route " +
            "to the upper terrace (lift available, button at reachable height with braille). Designated seating with " +
            "good sightlines, and quiet hours posted for sensory-sensitive visitors. Parking bay close to the entrance. " +
            "Overall a thoughtfully accessible experience that I would recommend to wheelchair users and visitors with " +
            "low vision alike. Minor note: one secondary corridor was a little narrow during peak times.";

        const string minContent = "Step-free, helpful."; // ≥ 10 chars — minimum valid content.

        return
        [
            // ── PLACE (Petra): multi-review list + ownership + content edges ─────
            // OWN + within 48h → edit AND delete controls visible to the tester.
            new SeedSpec(UserMe, ReviewTargetType.Place, PlacePetra, 4.5m,
                "FE2D Accessibility Review – editable (own, <48h)",
                "Own review created within 48 hours. Edit and delete controls should both be visible.",
                DaysAgo(2), "Wheelchair,Mobility", HoursAgo(3)),

            // OTHER user → all six features; title + visit date present.
            new SeedSpec(TravelerOne, ReviewTargetType.Place, PlacePetra, 5.0m,
                "FE2D Accessibility Review – other user (full features)",
                "Another user owns this review. The tester can view it but must not see edit/delete controls.",
                DaysAgo(20), "Wheelchair,Visual,Hearing,Cognitive,Mobility,Other", HoursAgo(72)),

            // OTHER user → title MISSING, visit date MISSING, single feature.
            new SeedSpec(TravelerTwo, ReviewTargetType.Place, PlacePetra, 3.5m,
                null,
                "FE2D Accessibility Review with no title and no visit date. Cognitive signage was clear and calm.",
                null, "Cognitive", HoursAgo(120)),

            // OTHER user → long-but-valid content, Hearing feature.
            new SeedSpec(TravelerThree, ReviewTargetType.Place, PlacePetra, 4.0m,
                "FE2D Accessibility Review – long content",
                longContent,
                DaysAgo(9), "Hearing", HoursAgo(200)),

            // ── TOUR (Petra Explorer): own (expired) + others ───────────────────
            // OWN + OLDER than 48h → edit control hidden/disabled; delete per backend rules.
            new SeedSpec(UserMe, ReviewTargetType.Tour, TourPetraExplorer, 4.0m,
                "FE2D Accessibility Review – expired window (own, >48h)",
                "Own review older than 48 hours. The edit control should be hidden; delete follows backend rules.",
                DaysAgo(10), "Wheelchair,Hearing", HoursAgo(72)),

            // OTHER user → Visual feature, minimum valid content, no title.
            new SeedSpec(TravelerFour, ReviewTargetType.Tour, TourPetraExplorer, 2.5m,
                null,
                minContent,
                DaysAgo(5), "Visual", HoursAgo(30)),

            // OTHER user → Other + Mobility, title present, visit date present.
            new SeedSpec(TravelerOne, ReviewTargetType.Tour, TourPetraExplorer, 4.5m,
                "FE2D Accessibility Review – guide adapted pace",
                "Guide adapted the walking pace and gave audio descriptions at each stop.",
                DaysAgo(12), "Other,Mobility", HoursAgo(60)),

            // ── BUSINESS (Petra Guides): own editable + other ───────────────────
            // OWN + within 48h → editable; Business is the one target with a direct My-Reviews link.
            new SeedSpec(UserMe, ReviewTargetType.Business, BusinessPetra, 5.0m,
                "FE2D Accessibility Review – editable business (own, <48h)",
                "Own business review within the edit window. Wheelchair access and accessible restroom confirmed.",
                DaysAgo(1), "Wheelchair", HoursAgo(6)),

            // OTHER user → multiple features.
            new SeedSpec(TravelerTwo, ReviewTargetType.Business, BusinessPetra, 4.0m,
                "FE2D Accessibility Review – business team",
                "Front-of-house staff were patient and knowledgeable about accessible routes and seating.",
                DaysAgo(14), "Mobility,Cognitive", HoursAgo(90)),

            // ── TOURGUIDE (FE2D Smoke Guide): own editable + other ──────────────
            // OWN + within 48h → editable on /guides/fe2d-smoke-guide.
            new SeedSpec(UserMe, ReviewTargetType.TourGuide, SmokeTourGuide, 4.5m,
                "FE2D Accessibility Review – editable guide (own, <48h)",
                "Own tour-guide accessibility review within the edit window. Guide knew every step-free route.",
                DaysAgo(1), "Mobility,Visual", HoursAgo(12)),

            // OTHER user → no title, single feature.
            new SeedSpec(TravelerThree, ReviewTargetType.TourGuide, SmokeTourGuide, 5.0m,
                null,
                "Patient, clear, and accommodating. Highly recommended for visitors with mobility needs.",
                DaysAgo(7), "Mobility", HoursAgo(150)),

            // ── PLACE (FE2D Smoke Place Full): own editable + other user ─────────
            // OWN + within 48h → editable on /places/fe2d-smoke-place-full.
            new SeedSpec(UserMe, ReviewTargetType.Place, SmokePlaceFull, 5.0m,
                "FE2D Accessibility Review – editable (own, <48h)",
                "Own accessibility review on the FE2D full place, within the 48h edit window. Step-free throughout.",
                DaysAgo(1), "Wheelchair,Visual,Hearing", HoursAgo(5)),

            // OTHER user → all features, long-ish content (renders alongside the owned one).
            new SeedSpec(TravelerOne, ReviewTargetType.Place, SmokePlaceFull, 4.5m,
                "FE2D Accessibility Review – full place by other user",
                "Another user's accessibility review on the FE2D full place. Helpful staff and clear braille signage.",
                DaysAgo(6), "Wheelchair,Mobility,Cognitive", HoursAgo(140)),
        ];
    }

    /// <summary>
    /// Materialise a <see cref="SeedSpec"/> into an <see cref="AccessibilityReview"/> with a fixed
    /// CreatedAt (drives the 48h edit-window UI) and Published status.
    /// </summary>
    private static AccessibilityReview Materialise(SeedSpec spec)
    {
        var review = CreateEntity<AccessibilityReview>();
        SetProperty(review, nameof(AccessibilityReview.UserId), spec.UserId);
        SetProperty(review, nameof(AccessibilityReview.TargetType), spec.TargetType);
        SetProperty(review, nameof(AccessibilityReview.TargetId), spec.TargetId);
        SetProperty(review, nameof(AccessibilityReview.Rating), spec.Rating);
        SetProperty(review, nameof(AccessibilityReview.Title), spec.Title);
        SetProperty(review, nameof(AccessibilityReview.Content), spec.Content);
        SetProperty(review, nameof(AccessibilityReview.VisitDate), spec.VisitDate);
        SetProperty(review, nameof(AccessibilityReview.FeatureTypesCsv), spec.FeatureTypesCsv);
        SetProperty(review, nameof(AccessibilityReview.Status), AccessibilityReviewStatus.Published);
        SetProperty(review, nameof(AccessibilityReview.IsDeleted), false);
        SetProperty(review, nameof(AccessibilityReview.CreatedAt), spec.CreatedAtUtc);
        return review;
    }

    private sealed record SeedSpec(
        Guid UserId,
        ReviewTargetType TargetType,
        Guid TargetId,
        decimal Rating,
        string? Title,
        string Content,
        DateOnly? VisitDate,
        string FeatureTypesCsv,
        DateTime CreatedAtUtc);

    private static TEntity CreateEntity<TEntity>() where TEntity : class
    {
        var entity = Activator.CreateInstance(typeof(TEntity), nonPublic: true) as TEntity;
        if (entity is null)
            throw new InvalidOperationException($"Failed to create entity instance for {typeof(TEntity).FullName}.");
        return entity;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (property is null)
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.GetType().FullName}.");

        property.SetValue(target, value);
    }
}
