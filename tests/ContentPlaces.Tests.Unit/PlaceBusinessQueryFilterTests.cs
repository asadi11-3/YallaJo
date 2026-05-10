using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-EF-FILTER-001 regression tests.
///
/// <para>
/// Verifies that the <see cref="PlaceBusiness"/> join entity has a query filter
/// matching the global soft-delete filters on its principals (<see cref="Place"/>
/// and <see cref="Business"/>), consistent with the standard already established
/// for ContentTours' <c>TourPackageTour</c> (EF-P1-001) and <c>TourTourGuide</c>
/// (P2-002).
/// </para>
///
/// <para>
/// Uses the EF InMemory provider via the same factory pattern as
/// <c>LanguageActivatedIntegrationEventHandlerTests</c>; no SQL Server connection
/// required.
/// </para>
/// </summary>
public sealed class PlaceBusinessQueryFilterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContentPlacesDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ContentPlacesDbContext>()
            .UseInMemoryDatabase(databaseName: $"placebusiness-filter-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentPlacesDbContext(options);
    }

    // ── 1. Model metadata — query filter must be registered ──────────────────

    [Fact]
    public void PlaceBusiness_EfModel_HasQueryFilterRegistered()
    {
        using var ctx = NewInMemoryContext();

        var entityType = ctx.Model.FindEntityType(typeof(PlaceBusiness));

        entityType.Should().NotBeNull(
            "PlaceBusiness must be registered in the EF model");

        entityType!.GetQueryFilter().Should().NotBeNull(
            "PlaceBusinessConfiguration must call HasQueryFilter so junction rows " +
            "linked to a soft-deleted Place or Business are excluded from default " +
            "queries, consistent with TourPackageTour (EF-P1-001) and TourTourGuide " +
            "(P2-002).");
    }

    // ── 2. Behaviour — rows linked to a soft-deleted Place are hidden ─────────

    [Fact]
    public async Task PlaceBusiness_SoftDeletedPlace_IsExcludedByDefaultQuery()
    {
        await using var ctx = NewInMemoryContext();

        var activePlace  = TestPlaceFactory.CreatePlace(Guid.NewGuid(), slug: "active");
        var deletedPlace = TestPlaceFactory.CreatePlace(Guid.NewGuid(), slug: "deleted");
        deletedPlace.SoftDelete();   // IsDeleted = true

        var business = TestBusinessFactory.CreateBusiness(Guid.NewGuid());

        var visibleLink = PlaceBusiness.Create(activePlace.Id,  business.Id);
        var hiddenLink  = PlaceBusiness.Create(deletedPlace.Id, business.Id);

        ctx.Places.AddRange(activePlace, deletedPlace);
        ctx.Businesses.Add(business);
        ctx.PlaceBusinesses.AddRange(visibleLink, hiddenLink);
        await ctx.SaveChangesAsync();

        // Default query — filter must suppress the row whose Place.IsDeleted = true.
        var visible = await ctx.PlaceBusinesses.AsNoTracking().ToListAsync();

        visible.Should().HaveCount(1,
            "the junction row linked to the soft-deleted Place should be filtered out");
        visible[0].PlaceId.Should().Be(activePlace.Id);
        visible[0].BusinessId.Should().Be(business.Id);
    }

    // ── 3. Behaviour — rows linked to a soft-deleted Business are hidden ─────

    [Fact]
    public async Task PlaceBusiness_SoftDeletedBusiness_IsExcludedByDefaultQuery()
    {
        await using var ctx = NewInMemoryContext();

        var place = TestPlaceFactory.CreatePlace(Guid.NewGuid());

        var activeBusiness  = TestBusinessFactory.CreateBusiness(Guid.NewGuid());
        var deletedBusiness = TestBusinessFactory.CreateBusiness(Guid.NewGuid());
        deletedBusiness.SoftDelete();   // IsDeleted = true

        var visibleLink = PlaceBusiness.Create(place.Id, activeBusiness.Id);
        var hiddenLink  = PlaceBusiness.Create(place.Id, deletedBusiness.Id);

        ctx.Places.Add(place);
        ctx.Businesses.AddRange(activeBusiness, deletedBusiness);
        ctx.PlaceBusinesses.AddRange(visibleLink, hiddenLink);
        await ctx.SaveChangesAsync();

        var visible = await ctx.PlaceBusinesses.AsNoTracking().ToListAsync();

        visible.Should().HaveCount(1,
            "the junction row linked to the soft-deleted Business should be filtered out");
        visible[0].PlaceId.Should().Be(place.Id);
        visible[0].BusinessId.Should().Be(activeBusiness.Id);
    }

    // ── 4. IgnoreQueryFilters surfaces all rows ───────────────────────────────

    [Fact]
    public async Task PlaceBusiness_IgnoreQueryFilters_ReturnsRowsLinkedToSoftDeletedParents()
    {
        await using var ctx = NewInMemoryContext();

        var activePlace     = TestPlaceFactory.CreatePlace(Guid.NewGuid(), slug: "active");
        var deletedPlace    = TestPlaceFactory.CreatePlace(Guid.NewGuid(), slug: "deleted");
        deletedPlace.SoftDelete();

        var activeBusiness  = TestBusinessFactory.CreateBusiness(Guid.NewGuid());
        var deletedBusiness = TestBusinessFactory.CreateBusiness(Guid.NewGuid());
        deletedBusiness.SoftDelete();

        // Three junction rows: one fully visible, two each touching a soft-deleted parent.
        var allActive   = PlaceBusiness.Create(activePlace.Id,  activeBusiness.Id);
        var deletedSide = PlaceBusiness.Create(deletedPlace.Id, activeBusiness.Id);
        var deletedBiz  = PlaceBusiness.Create(activePlace.Id,  deletedBusiness.Id);

        ctx.Places.AddRange(activePlace, deletedPlace);
        ctx.Businesses.AddRange(activeBusiness, deletedBusiness);
        ctx.PlaceBusinesses.AddRange(allActive, deletedSide, deletedBiz);
        await ctx.SaveChangesAsync();

        // Default query — only the fully-active junction row is visible.
        var defaultRows = await ctx.PlaceBusinesses.AsNoTracking().ToListAsync();
        defaultRows.Should().HaveCount(1);

        // IgnoreQueryFilters bypasses the filter — all 3 rows visible.
        var allRows = await ctx.PlaceBusinesses.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        allRows.Should().HaveCount(3,
            "IgnoreQueryFilters must return every row regardless of the parents' IsDeleted state");
    }
}
