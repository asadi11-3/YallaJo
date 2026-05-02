using System.Linq.Expressions;
using ContentTours.Application.Caching;
using ContentTours.Application.Queries.ChildrenInfo.GetTourChildrenInfo;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Behaviour tests for <see cref="GetTourChildrenInfoQueryHandler"/>.
/// Covers DTO mapping, deterministic order, NotFound, and query cache contract.
/// </summary>
public sealed class GetTourChildrenInfoQueryHandlerTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        GetTourChildrenInfoQueryHandler Handler,
        ITourRepository TourRepo)
        Build()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var logger = Substitute.For<ILogger<GetTourChildrenInfoQueryHandler>>();
        var handler = new GetTourChildrenInfoQueryHandler(tourRepo, logger);
        return (handler, tourRepo);
    }

    // ── stub helpers ──────────────────────────────────────────────────────────

    private static void StubGetAsync(ITourRepository repo, Tour? tour)
        => repo.GetAsync(
                filter: Arg.Any<Expression<Func<Tour, bool>>>(),
                include: Arg.Any<Func<IQueryable<Tour>, IQueryable<Tour>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct: Arg.Any<CancellationToken>())
            .Returns(tour);

    // ── DTO mapping + facility names ─────────────────────────────────────────

    [Fact]
    public async Task Get_HappyPath_ReturnsAllowsChildrenAgesAndFacilityNames()
    {
        var (handler, repo) = Build();
        var tour = TestTourFactory.CreateDraft();
        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 4,
            maxChildAge: 9,
            childFacilities: [ChildFacility.Stroller, ChildFacility.PlayArea]);

        StubGetAsync(repo, tour);

        var result = await handler.Handle(new GetTourChildrenInfoQuery(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AllowsChildren.Should().BeTrue();
        result.Value.MinChildAge.Should().Be(4);
        result.Value.MaxChildAge.Should().Be(9);
        result.Value.ChildFacilities.Should().Equal("Stroller", "PlayArea");
    }

    // ── deterministic order ───────────────────────────────────────────────────

    [Fact]
    public async Task Get_FacilitiesOrderedDeterministicallyByEnumValue()
    {
        var (handler, repo) = Build();
        var tour = TestTourFactory.CreateDraft();
        tour.UpdateChildrenInfo(
            allowsChildren: true,
            minChildAge: 5,
            maxChildAge: 12,
            childFacilities: [ChildFacility.AirConditioning, ChildFacility.ChildSeat, ChildFacility.Stroller]);

        StubGetAsync(repo, tour);

        var result = await handler.Handle(new GetTourChildrenInfoQuery(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChildFacilities.Should().Equal("Stroller", "ChildSeat", "AirConditioning");
    }

    // ── Tour.NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_TourMissing_Returns404TourNotFound()
    {
        var (handler, repo) = Build();
        StubGetAsync(repo, tour: null);

        var result = await handler.Handle(new GetTourChildrenInfoQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    [Fact]
    public async Task Get_DeletedTour_Returns404TourNotFound()
    {
        var (handler, repo) = Build();
        var tour = TestTourFactory.CreateDraft();
        tour.SoftDelete();
        StubGetAsync(repo, tour);

        var result = await handler.Handle(new GetTourChildrenInfoQuery(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ICacheableQuery contract ──────────────────────────────────────────────

    [Fact]
    public void Query_ImplementsICacheableQuery_WithExpectedKeyAndTags()
    {
        var tourId = Guid.NewGuid();
        var query = new GetTourChildrenInfoQuery(tourId);
        var cacheable = query.Should().BeAssignableTo<ICacheableQuery>().Subject;

        cacheable.CacheKey.Should().Be(TourChildrenInfoCacheKeys.Get(tourId));
        cacheable.CacheDuration.Should().Be(TimeSpan.FromMinutes(10));
        cacheable.Tags.Should().Contain(TourChildrenInfoCacheKeys.TagForTour(tourId));
        cacheable.Tags.Should().Contain(ContentToursCacheKeys.TagForTour(tourId));
    }
}
