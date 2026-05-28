using ContentTours.Application.Queries.Tour.ListFeaturedTours;
using ContentTours.Application.Queries.Tour.ListMyTours;
using ContentTours.Application.Queries.Tour.SearchTours;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// EF-InMemory integration tests for SearchTours / Featured / MyTours query handlers.
/// Tests pin filter behaviour, status filtering, and provider-scoping rules from PDF Task-3.
///
/// Search relevance scoring is intentionally NOT pinned here — see ADR-005 (deferred).
/// </summary>
public sealed class TourSearchQueryHandlerTests
{
    private static Tour SeedTour(
        string name,
        TourStatus status = TourStatus.Approved,
        bool isFeatured = false,
        bool isDeleted = false,
        bool isChildFriendly = false,
        bool isAccessible = false,
        bool isInstantBooking = false,
        decimal price = 100m,
        decimal averageRating = 4.0m,
        int bookingCount = 0,
        Guid? createdBy = null,
        Guid? placeId = null)
    {
        var tour = Tour.Create(
            name:                    name,
            slug:                    $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}",
            difficulty:              Difficulty.Easy,
            durationMinutes:         480,
            maxGroupSize:            20,
            basePriceAmount:         price,
            currency:                "JOD",
            location:                new Location(30.32m, 35.45m),
            createdByUserId:         createdBy ?? Guid.NewGuid(),
            description:             new string('a', 120),
            shortDescription:        null,
            minAge:                  null,
            meetingPoint:            new Location(30.32m, 35.45m),
            placeId:                 placeId ?? Guid.NewGuid(),
            isChildFriendly:         isChildFriendly,
            isAccessible:            isAccessible,
            isInstantBooking:        isInstantBooking);

        // Drive aggregate lifecycle to the requested status.
        if (status != TourStatus.Draft)
        {
            tour.Submit();
            if (status == TourStatus.Pending) { /* stop here */ }
            else if (status == TourStatus.Approved) tour.Approve(Guid.NewGuid());
            else if (status == TourStatus.Rejected) tour.Reject("test", Guid.NewGuid());
            else if (status == TourStatus.Suspended)
            {
                tour.Approve(Guid.NewGuid());
                tour.Suspend("test");
            }
        }

        if (isFeatured && tour.Status == TourStatus.Approved)
            tour.SetFeatured(true, Guid.NewGuid());

        if (averageRating > 0)
            tour.UpdateRating(averageRating, reviewCount: 1);

        if (bookingCount > 0)
            tour.UpdateBookingCount(bookingCount);

        if (isDeleted)
            tour.SoftDelete();

        return tour;
    }

    // ── Search ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Search_WithEmptyQAndNoFilters_ReturnsTourSearchQueryRequired()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        var handler = new SearchToursQueryHandler(
            new TourRepository(db),
            Substitute.For<ILogger<SearchToursQueryHandler>>());

        var result = await handler.Handle(
            new SearchToursQuery(new SearchToursRequest(
                Q: null,
                PlaceId: null, PriceMin: null, PriceMax: null,
                Difficulty: null, DurationMinutesMin: null, DurationMinutesMax: null,
                IsChildFriendly: null, IsAccessible: null, IsInstantBooking: null,
                HasDiscount: null, MinRating: null, LanguageCode: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.SearchQueryRequired");
    }

    [Fact]
    public async Task Search_OnlyReturnsApprovedAndNonDeletedTours()
    {
        // NOTE: SearchToursQueryHandler uses lower-cased tokens with .Contains(token).
        // The production SQL Server collation is CI/AI, but EF Core InMemory does
        // case-sensitive .Contains. We seed tour names in lowercase to keep this test
        // independent of provider-collation behaviour.
        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("petra day tour"),                                    // Approved
            SeedTour("petra draft",     status: TourStatus.Draft),
            SeedTour("petra pending",   status: TourStatus.Pending),
            SeedTour("petra suspended", status: TourStatus.Suspended),
            SeedTour("petra approved deleted", isDeleted: true));
        await db.SaveChangesAsync();

        var handler = new SearchToursQueryHandler(
            new TourRepository(db),
            Substitute.For<ILogger<SearchToursQueryHandler>>());

        var result = await handler.Handle(
            new SearchToursQuery(new SearchToursRequest(
                Q: "petra",
                PlaceId: null, PriceMin: null, PriceMax: null,
                Difficulty: null, DurationMinutesMin: null, DurationMinutesMax: null,
                IsChildFriendly: null, IsAccessible: null, IsInstantBooking: null,
                HasDiscount: null, MinRating: null, LanguageCode: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "petra day tour" });
    }

    [Fact]
    public async Task Search_FiltersByPlaceId()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        db.Tours.AddRange(
            SeedTour("In place A", placeId: placeA),
            SeedTour("Also in place A", placeId: placeA),
            SeedTour("In place B", placeId: placeB));
        await db.SaveChangesAsync();

        var handler = new SearchToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<SearchToursQueryHandler>>());

        var result = await handler.Handle(
            new SearchToursQuery(new SearchToursRequest(
                Q: null,
                PlaceId: placeA,
                PriceMin: null, PriceMax: null,
                Difficulty: null, DurationMinutesMin: null, DurationMinutesMax: null,
                IsChildFriendly: null, IsAccessible: null, IsInstantBooking: null,
                HasDiscount: null, MinRating: null, LanguageCode: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value!.Items.Should().OnlyContain(i => i.Name.Contains("place A"));
    }

    [Fact]
    public async Task Search_FiltersByPriceRange()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("Cheap",  price: 10m),
            SeedTour("Mid",    price: 100m),
            SeedTour("Pricey", price: 500m));
        await db.SaveChangesAsync();

        var handler = new SearchToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<SearchToursQueryHandler>>());

        var result = await handler.Handle(
            new SearchToursQuery(new SearchToursRequest(
                Q: null,
                PlaceId: null, PriceMin: 50m, PriceMax: 200m,
                Difficulty: null, DurationMinutesMin: null, DurationMinutesMax: null,
                IsChildFriendly: null, IsAccessible: null, IsInstantBooking: null,
                HasDiscount: null, MinRating: null, LanguageCode: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Mid" });
    }

    [Fact]
    public async Task Search_FiltersByChildFriendlyAndMinRating()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("Family", isChildFriendly: true,  averageRating: 4.5m),
            SeedTour("Adults only", isChildFriendly: false, averageRating: 4.5m),
            SeedTour("Family low rating", isChildFriendly: true, averageRating: 3.0m));
        await db.SaveChangesAsync();

        var handler = new SearchToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<SearchToursQueryHandler>>());

        var result = await handler.Handle(
            new SearchToursQuery(new SearchToursRequest(
                Q: null, PlaceId: null, PriceMin: null, PriceMax: null,
                Difficulty: null, DurationMinutesMin: null, DurationMinutesMax: null,
                IsChildFriendly: true, IsAccessible: null, IsInstantBooking: null,
                HasDiscount: null, MinRating: 4.0m, LanguageCode: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Family" });
    }

    // ── Featured ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Featured_ReturnsOnlyApprovedFeaturedNonDeleted_OrderedByPopularity()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("F-A", isFeatured: true, bookingCount: 50, averageRating: 4.5m),
            SeedTour("F-B", isFeatured: true, bookingCount: 100, averageRating: 4.0m),
            SeedTour("F-C", isFeatured: true, bookingCount: 50, averageRating: 4.8m),
            SeedTour("Not featured", isFeatured: false, bookingCount: 999),
            SeedTour("F-Deleted", isFeatured: true, isDeleted: true, bookingCount: 999));
        await db.SaveChangesAsync();

        var handler = new ListFeaturedToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<ListFeaturedToursQueryHandler>>());

        var result = await handler.Handle(new ListFeaturedToursQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var names = result.Value!.Select(i => i.Name).ToList();
        names.Should().NotContain(new[] { "Not featured", "F-Deleted" });
        // Sort: BookingCount DESC, AverageRating DESC → F-B, then F-C (50/4.8) before F-A (50/4.5)
        names.Should().Equal(new[] { "F-B", "F-C", "F-A" });
    }

    // ── MyTours ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task MyTours_ProviderSeesOwnToursAcrossAllStatuses()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();

        await using var db = TestDbContextFactory.NewInMemory();
        db.Tours.AddRange(
            SeedTour("Mine Approved", createdBy: owner),
            SeedTour("Mine Draft", status: TourStatus.Draft, createdBy: owner),
            SeedTour("Mine Pending", status: TourStatus.Pending, createdBy: owner),
            SeedTour("Other's Approved", createdBy: other));
        await db.SaveChangesAsync();

        var handler = new ListMyToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<ListMyToursQueryHandler>>());

        var result = await handler.Handle(new ListMyToursQuery(
            EffectiveUserId: owner,
            Page: 1, PageSize: 50,
            StatusFilter: null, Sort: null, IncludeDeleted: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(3);
        result.Value!.Items.Select(i => i.Name).Should().BeEquivalentTo(new[]
        {
            "Mine Approved", "Mine Draft", "Mine Pending"
        });
    }

    [Fact]
    public async Task MyTours_InvalidStatusFilter_ReturnsTourInvalidStatusFilter()
    {
        await using var db = TestDbContextFactory.NewInMemory();
        var handler = new ListMyToursQueryHandler(new TourRepository(db),
            Substitute.For<ILogger<ListMyToursQueryHandler>>());

        var result = await handler.Handle(new ListMyToursQuery(
            EffectiveUserId: Guid.NewGuid(),
            Page: 1, PageSize: 50,
            StatusFilter: "NotARealStatus", Sort: null, IncludeDeleted: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.InvalidStatusFilter");
    }
}
