using ContentPlaces.Application.Queries.Place.GetMapViewport;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// Regression tests for P1-009: GetMapViewportQueryHandler caps results at 500 pins,
/// projects server-side, sorts featured-first then by rating, and signals clustering
/// when the cap is reached. Uses an in-memory async-aware IQueryable provider so the
/// handler's <c>ToListAsync</c> call works without a real DbContext.
/// </summary>
public sealed class GetMapViewportQueryHandlerTests
{
    private const int MaxPins = 500;
    private const decimal SouthLat = 31m;
    private const decimal NorthLat = 33m;
    private const decimal WestLng = 35m;
    private const decimal EastLng = 36m;

    private static GetMapViewportQuery BuildViewportQuery() =>
        new(
            NorthLat: (double)NorthLat,
            SouthLat: (double)SouthLat,
            EastLng: (double)EastLng,
            WestLng: (double)WestLng);

    private static (
        GetMapViewportQueryHandler Handler,
        IPlaceRepository PlaceRepo) BuildSubject(IEnumerable<PlaceEntity> places)
    {
        var placeRepo = Substitute.For<IPlaceRepository>();
        var logger = Substitute.For<ILogger<GetMapViewportQueryHandler>>();

        var queryable = new TestAsyncEnumerable<PlaceEntity>(places);
        placeRepo.Query(
            filter: Arg.Any<System.Linq.Expressions.Expression<Func<PlaceEntity, bool>>?>(),
            include: Arg.Any<Func<IQueryable<PlaceEntity>, IQueryable<PlaceEntity>>?>(),
            asNoTracking: Arg.Any<bool>())
            .Returns(queryable);

        var handler = new GetMapViewportQueryHandler(placeRepo, logger);
        return (handler, placeRepo);
    }

    private static PlaceEntity CreatePlaceInsideViewport(
        string name = "Inside",
        decimal lat = 32m,
        decimal lng = 35.5m,
        bool featured = false,
        decimal rating = 0m,
        int reviewCount = 0)
    {
        var place = TestPlaceFactory.CreatePlace(
            createdByUserId: Guid.NewGuid(),
            name: name,
            slug: $"slug-{Guid.NewGuid():N}",
            latitude: lat,
            longitude: lng);

        if (featured) place.SetFeatured(true);
        if (rating > 0m && reviewCount > 0) place.UpdateRating(rating, reviewCount);

        return place;
    }

    [Fact]
    public async Task ReturnsAllInsideViewportWhenBelowCap()
    {
        var places = Enumerable.Range(0, 10)
            .Select(i => CreatePlaceInsideViewport(name: $"P{i}", lat: 32m + (decimal)(i * 0.001), lng: 35.5m))
            .ToList();
        var (handler, _) = BuildSubject(places);

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().HaveCount(10);
        result.Value!.IsClusteringRecommended.Should().BeFalse();
    }

    [Fact]
    public async Task IsClusteringRecommendedWhenNaturalThresholdExceededButBelowCap()
    {
        // 51 pins — > 50 triggers natural clustering hint, no cap hit.
        var places = Enumerable.Range(0, 51)
            .Select(i => CreatePlaceInsideViewport(name: $"P{i}", lat: 32m + (decimal)(i * 0.0001), lng: 35.5m))
            .ToList();
        var (handler, _) = BuildSubject(places);

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().HaveCount(51);
        result.Value!.IsClusteringRecommended.Should().BeTrue();
    }

    [Fact]
    public async Task CapsAtMaxPinsAndFlagsClusteringRecommended()
    {
        // 600 pins inside the viewport — handler should cap at 500.
        var places = Enumerable.Range(0, 600)
            .Select(i => CreatePlaceInsideViewport(name: $"P{i:D4}", lat: 32m + (decimal)(i * 0.00001), lng: 35.5m))
            .ToList();
        var (handler, _) = BuildSubject(places);

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().HaveCount(MaxPins);
        result.Value!.IsClusteringRecommended.Should().BeTrue();
    }

    [Fact]
    public async Task ExcludesPlacesOutsideViewport()
    {
        var inside = CreatePlaceInsideViewport(name: "Inside", lat: 32m, lng: 35.5m);
        var north = CreatePlaceInsideViewport(name: "TooFarNorth", lat: 40m, lng: 35.5m);
        var south = CreatePlaceInsideViewport(name: "TooFarSouth", lat: 20m, lng: 35.5m);
        var east  = CreatePlaceInsideViewport(name: "TooFarEast",  lat: 32m, lng: 50m);
        var west  = CreatePlaceInsideViewport(name: "TooFarWest",  lat: 32m, lng: 10m);

        var (handler, _) = BuildSubject(new[] { inside, north, south, east, west });

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().HaveCount(1);
        result.Value!.Pins[0].Name.Should().Be("Inside");
    }

    [Fact]
    public async Task ExcludesPlacesAtZeroCoordinates()
    {
        // Defensive filter: the handler skips coordinates of (0, 0) which signal
        // legacy/unset rows and would all collapse onto one point.
        var inside = CreatePlaceInsideViewport(name: "Real", lat: 32m, lng: 35.5m);

        // Build a Place with explicit (0, 0) location.
        var unset = TestPlaceFactory.CreatePlace(
            createdByUserId: Guid.NewGuid(),
            name: "Unset",
            slug: $"unset-{Guid.NewGuid():N}",
            latitude: 0m,
            longitude: 0m);

        var (handler, _) = BuildSubject(new[] { inside, unset });

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().ContainSingle();
        result.Value!.Pins[0].Name.Should().Be("Real");
    }

    [Fact]
    public async Task OrdersFeaturedFirstThenByRatingDescending()
    {
        var lowRatedNotFeatured = CreatePlaceInsideViewport(name: "Low",      rating: 1m, reviewCount: 1);
        var midRatedNotFeatured = CreatePlaceInsideViewport(name: "Mid",      rating: 3m, reviewCount: 5);
        var highRatedNotFeatured = CreatePlaceInsideViewport(name: "HighOnly", rating: 4.8m, reviewCount: 100);
        var featuredLowRating = CreatePlaceInsideViewport(name: "Featured", featured: true, rating: 2m, reviewCount: 2);

        var (handler, _) = BuildSubject(new[]
        {
            lowRatedNotFeatured, midRatedNotFeatured, highRatedNotFeatured, featuredLowRating,
        });

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Pins.Should().HaveCount(4);

        // Featured first.
        result.Value!.Pins[0].Name.Should().Be("Featured");
        // Then non-featured by rating descending.
        result.Value!.Pins[1].Name.Should().Be("HighOnly");
        result.Value!.Pins[2].Name.Should().Be("Mid");
        result.Value!.Pins[3].Name.Should().Be("Low");
    }

    [Fact]
    public async Task ResponseShapeUnchanged()
    {
        // Lock the public response shape: { Pins, IsClusteringRecommended } — no pageSize/limit field.
        var places = new[] { CreatePlaceInsideViewport() };
        var (handler, _) = BuildSubject(places);

        var result = await handler.Handle(BuildViewportQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var responseType = typeof(MapViewportResponse);
        var publicProps = responseType
            .GetProperties()
            .Select(p => p.Name)
            .OrderBy(s => s)
            .ToArray();

        publicProps.Should().BeEquivalentTo(new[] { "IsClusteringRecommended", "Pins" });
    }
}
