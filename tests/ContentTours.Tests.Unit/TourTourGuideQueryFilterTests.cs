using ContentTours.Domain.Entities;
using ContentTours.Tests.Unit.Mohammad;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Regression tests for CONTENTTOURS-STD-P2-002.
///
/// Verifies that <see cref="TourTourGuide"/> has a query filter matching the
/// global soft-delete filter on its principal (<see cref="Tour"/>), consistent
/// with all other Tour child/join entities (TourSchedule, TourWaypoint,
/// TourPricingTier, TourTranslation, TourChildFacility, TourPackageTour).
///
/// Uses the EF InMemory provider via <see cref="TestDbContextFactory"/>; no
/// SQL Server connection required.
/// </summary>
public sealed class TourTourGuideQueryFilterTests
{
    // ── 1. Model metadata — query filter must be registered ──────────────────

    [Fact]
    public void TourTourGuide_EfModel_HasQueryFilterRegistered()
    {
        using var ctx = TestDbContextFactory.NewInMemory();

        var entityType = ctx.Model.FindEntityType(typeof(TourTourGuide));

        entityType.Should().NotBeNull(
            "TourTourGuide must be registered in the EF model");

        entityType!.GetQueryFilter().Should().NotBeNull(
            "TourTourGuideConfiguration must call HasQueryFilter so guide-assignment " +
            "rows linked to a soft-deleted Tour are excluded from default queries, " +
            "consistent with TourSchedule, TourWaypoint, TourPricingTier, etc.");
    }

    // ── 2. Behaviour — rows linked to a soft-deleted Tour are hidden ─────────

    [Fact]
    public async Task TourTourGuide_SoftDeletedTour_IsExcludedByDefaultQuery()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var activeTour  = TestTourFactory.CreateDraft();
        var deletedTour = TestTourFactory.CreateDraft();
        deletedTour.SoftDelete();                      // IsDeleted = true

        var guideId = Guid.NewGuid();

        var activeLink  = TourTourGuide.Create(activeTour.Id,  guideId, isPrimary: true);
        var deletedLink = TourTourGuide.Create(deletedTour.Id, guideId, isPrimary: true);

        ctx.Tours.AddRange(activeTour, deletedTour);
        ctx.TourTourGuides.AddRange(activeLink, deletedLink);
        await ctx.SaveChangesAsync();

        // Default query — filter must suppress the row whose Tour.IsDeleted = true.
        var visible = await ctx.TourTourGuides.ToListAsync();

        visible.Should().HaveCount(1,
            "the guide-assignment row linked to the soft-deleted Tour should be filtered out");
        visible[0].TourId.Should().Be(activeTour.Id);
    }

    // ── 3. IgnoreQueryFilters surfaces all rows ───────────────────────────────

    [Fact]
    public async Task TourTourGuide_IgnoreQueryFilters_ReturnsRowsLinkedToSoftDeletedTour()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var activeTour  = TestTourFactory.CreateDraft();
        var deletedTour = TestTourFactory.CreateDraft();
        deletedTour.SoftDelete();

        var guideId = Guid.NewGuid();

        var activeLink  = TourTourGuide.Create(activeTour.Id,  guideId, isPrimary: true);
        var deletedLink = TourTourGuide.Create(deletedTour.Id, guideId, isPrimary: true);

        ctx.Tours.AddRange(activeTour, deletedTour);
        ctx.TourTourGuides.AddRange(activeLink, deletedLink);
        await ctx.SaveChangesAsync();

        // IgnoreQueryFilters bypasses the filter — both rows visible.
        var all = await ctx.TourTourGuides.IgnoreQueryFilters().ToListAsync();

        all.Should().HaveCount(2,
            "IgnoreQueryFilters must return every row regardless of the parent Tour's IsDeleted state");
    }
}
