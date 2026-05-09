using ContentTours.Domain.Entities;
using ContentTours.Tests.Unit.Mohammad;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Regression tests for EF-P1-001.
///
/// Verifies that <see cref="TourPackageTour"/> has a matching query filter
/// mirroring the global soft-delete filters on its two principals
/// (<see cref="Tour"/> and <see cref="TourPackage"/>), eliminating the EF
/// runtime warning:
///   "Entity 'Tour' has a global query filter defined and is the required end
///    of a relationship with the entity 'TourPackageTour'."
///
/// Uses the EF InMemory provider via <see cref="TestDbContextFactory"/>; no
/// SQL Server connection required.  Model metadata is evaluated identically by
/// InMemory, so the metadata assertion is binding.
/// </summary>
public sealed class TourPackageTourQueryFilterTests
{
    // ── 1. Model metadata — query filter must be registered ──────────────────

    [Fact]
    public void TourPackageTour_EfModel_HasQueryFilterRegistered()
    {
        using var ctx = TestDbContextFactory.NewInMemory();

        var entityType = ctx.Model.FindEntityType(typeof(TourPackageTour));

        entityType.Should().NotBeNull(
            "TourPackageTour must be registered in the EF model");

        entityType!.GetQueryFilter().Should().NotBeNull(
            "TourPackageTourConfiguration must call HasQueryFilter so the warning " +
            "'required end of a relationship has a global query filter' is eliminated");
    }

    // ── 2. Behaviour — soft-deleted Tour's link is hidden by default ─────────

    [Fact]
    public async Task TourPackageTour_SoftDeletedTour_IsExcludedByDefaultQuery()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var activeTour  = TestTourFactory.CreateDraft();
        var deletedTour = TestTourFactory.CreateDraft();
        deletedTour.SoftDelete();                                         // IsDeleted = true

        // TourPackage.Create stores TourPackageTour rows in _includedTours;
        // ctx.TourPackages.Add cascades them into the change tracker.
        var package = MakePackage(false, activeTour.Id, deletedTour.Id);

        ctx.Tours.AddRange(activeTour, deletedTour);
        ctx.TourPackages.Add(package);
        await ctx.SaveChangesAsync();

        // Default query — filter must suppress the link whose Tour.IsDeleted = true.
        var visible = await ctx.TourPackageTours.ToListAsync();

        visible.Should().HaveCount(1,
            "the junction row linked to the soft-deleted Tour should be filtered out");
        visible[0].TourId.Should().Be(activeTour.Id);
    }

    // ── 3. Behaviour — soft-deleted TourPackage's links are hidden ───────────

    [Fact]
    public async Task TourPackageTour_SoftDeletedPackage_IsExcludedByDefaultQuery()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var tour1 = TestTourFactory.CreateDraft();
        var tour2 = TestTourFactory.CreateDraft();

        var activePackage  = MakePackage(false, tour1.Id, tour2.Id);
        var deletedPackage = MakePackage(true,  tour1.Id, tour2.Id);   // IsDeleted = true

        ctx.Tours.AddRange(tour1, tour2);
        ctx.TourPackages.AddRange(activePackage, deletedPackage);
        await ctx.SaveChangesAsync();

        // Two packages, each with 2 links = 4 total rows; filter hides 2 (deleted package).
        var visible = await ctx.TourPackageTours.ToListAsync();

        visible.Should().HaveCount(2,
            "only links whose TourPackage is active should be visible; " +
            "the 2 links belonging to the soft-deleted package must be filtered out");
        visible.Should().AllSatisfy(link =>
            link.TourPackageId.Should().Be(activePackage.Id));
    }

    // ── 4. IgnoreQueryFilters surfaces all rows ───────────────────────────────

    [Fact]
    public async Task TourPackageTour_IgnoreQueryFilters_ReturnsAllRows()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var activeTour  = TestTourFactory.CreateDraft();
        var deletedTour = TestTourFactory.CreateDraft();
        deletedTour.SoftDelete();

        var package = MakePackage(false, activeTour.Id, deletedTour.Id);

        ctx.Tours.AddRange(activeTour, deletedTour);
        ctx.TourPackages.Add(package);
        await ctx.SaveChangesAsync();

        // IgnoreQueryFilters bypasses the filter — all 2 rows visible.
        var all = await ctx.TourPackageTours.IgnoreQueryFilters().ToListAsync();

        all.Should().HaveCount(2,
            "IgnoreQueryFilters must return every row regardless of IsDeleted state");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal valid <see cref="TourPackage"/> using the actual Tour IDs so
    /// that the internally built <see cref="TourPackageTour"/> junction rows reference
    /// real tracked entities.  EF cascade-adds the junction rows when the package is
    /// passed to <c>ctx.TourPackages.Add(package)</c>.
    /// </summary>
    private static TourPackage MakePackage(bool isDeleted, Guid tourId1, Guid tourId2)
    {
        var package = TourPackage.Create(
            name:                  "Test Package",
            description:           null,
            price:                 new Money(100m, "JOD"),
            currency:              "JOD",
            maxParticipants:       null,
            validFrom:             null,
            validTo:               null,
            createdByUserId:       Guid.NewGuid(),
            includedTourIds:       [tourId1, tourId2],
            inclusionDescriptions: []);

        if (isDeleted)
            package.SoftDelete();

        return package;
    }
}
