using System.Linq.Expressions;
using ContentTours.Application.Commands.TourWaypoints.AddTourWaypoint;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Behaviour tests for <see cref="AddTourWaypointCommandHandler"/>.
/// Covers PDF Critical Rules 2, 5, 6, 7, 9, 11, 12 and PDF Task 4A B1.
/// </summary>
public sealed class TourWaypointAddTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        AddTourWaypointCommandHandler Handler,
        ITourRepository TourRepo,
        ITourWaypointRepository WaypointRepo,
        IContentToursUnitOfWork Uow,
        HybridCache Cache,
        ICurrentUser CurrentUser)
        Build()
    {
        var tourRepo     = Substitute.For<ITourRepository>();
        var waypointRepo = Substitute.For<ITourWaypointRepository>();
        var uow          = Substitute.For<IContentToursUnitOfWork>();
        var cache        = Substitute.For<HybridCache>();
        var currentUser  = Substitute.For<ICurrentUser>();
        var logger       = Substitute.For<ILogger<AddTourWaypointCommandHandler>>();

        var handler = new AddTourWaypointCommandHandler(
            tourRepo, waypointRepo, uow, cache, currentUser, logger);
        return (handler, tourRepo, waypointRepo, uow, cache, currentUser);
    }

    // ── stub helpers ──────────────────────────────────────────────────────────

    /// <summary>Stubs <c>GetAllAsync</c> (the invariant-check call).</summary>
    private static void StubGetAll(ITourWaypointRepository repo, params TourWaypoint[] waypoints)
        => repo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourWaypoint, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourWaypoint>, IQueryable<TourWaypoint>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourWaypoint>, IOrderedQueryable<TourWaypoint>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
           .Returns(waypoints.ToList());

    /// <summary>Stubs <c>Query</c> (the server-side MaxAsync sort-order call).</summary>
    private static void StubQuery(ITourWaypointRepository repo, params TourWaypoint[] waypoints)
        => repo.Query(
                Arg.Any<Expression<Func<TourWaypoint, bool>>>(),
                Arg.Any<Func<IQueryable<TourWaypoint>, IQueryable<TourWaypoint>>?>(),
                Arg.Any<bool>())
           .Returns(new TestAsyncQueryable<TourWaypoint>(waypoints));

    private static AddTourWaypointCommand StandardCmd(Guid tourId, string name = "Petra Gate",
        WaypointType type = WaypointType.Stop)
        => new(tourId, name, null, 30.5d, 35.5d, type, null);

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_HappyPath_FirstWaypoint_Returns201WithSortOrderZero()
    {
        // Arrange
        var (handler, tourRepo, waypointRepo, _, cache, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns(tour);
        StubGetAll(waypointRepo);      // no existing waypoints
        StubQuery(waypointRepo);       // empty → MaxAsync returns null → SortOrder = 0

        // Act
        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.SortOrder.Should().Be(0);
        result.Value.WaypointId.Should().NotBeEmpty();
        await waypointRepo.Received(1).AddAsync(
            Arg.Is<TourWaypoint>(w => w.SortOrder == 0 && w.TourId == tour.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_HappyPath_SecondWaypoint_Returns201WithSortOrderOne()
    {
        // Arrange
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existing = TestWaypointFactory.Create(tour.Id, name: "Start Point",
            waypointType: WaypointType.Start, sortOrder: 0);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns(tour);
        StubGetAll(waypointRepo, existing);        // existing Start
        StubQuery(waypointRepo, existing);         // MaxAsync = 0 → newSortOrder = 1

        // Act
        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.SortOrder.Should().Be(1);
        await waypointRepo.Received(1).AddAsync(
            Arg.Is<TourWaypoint>(w => w.SortOrder == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_HappyPath_CacheTagsInvalidated()
    {
        // Verify PDF Critical Rule 9: most-specific tag first (ERR-010).
        var (handler, tourRepo, waypointRepo, _, cache, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);
        StubQuery(waypointRepo);

        await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            $"tour-waypoints:{tour.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            $"tour:{tour.Id}", Arg.Any<CancellationToken>());
    }

    // ── Tour.NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, _, _) = Build();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns((Domain.Entities.Tour?)null);

        var result = await handler.Handle(StandardCmd(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ownership gate ────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        // PDF Critical Rule 2: ICurrentUser only for ownership / IDOR.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);

        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Add_AdminNotOwner_Succeeds()
    {
        // Admin can act on tours they don't own (canonical gate per Mahmoud:45-52).
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(admin);
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);
        StubQuery(waypointRepo);

        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── (0,0) location guard ──────────────────────────────────────────────────

    [Fact]
    public async Task Add_ZeroZeroCoords_Returns422InvalidLocation()
    {
        // PDF B1.1: (0,0) is the Gulf of Guinea sentinel; reject always.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);

        var cmd = new AddTourWaypointCommand(tour.Id, "Zero Island", null,
            Latitude: 0d, Longitude: 0d,
            WaypointType: WaypointType.Stop,
            DurationMinutes: null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.InvalidLocation");
    }

    // ── name uniqueness (case-insensitive) ────────────────────────────────────

    [Fact]
    public async Task Add_DuplicateNameCaseInsensitive_Returns409NameConflict()
    {
        // B-4 decision: in-memory OrdinalIgnoreCase after single GetAllAsync materialization.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existing = TestWaypointFactory.Create(tour.Id, name: "Petra Gate");

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, existing);

        var cmd = StandardCmd(tour.Id, name: "petra gate"); // different casing

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.NameConflict");
    }

    // ── Single-Start invariant ─────────────────────────────────────────────────

    [Fact]
    public async Task Add_WhenStartAlreadyExists_Returns409InvalidType()
    {
        // PDF B1: exactly one Start waypoint per tour.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existing = TestWaypointFactory.Create(tour.Id, name: "Tour Start",
            waypointType: WaypointType.Start);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, existing);

        var result = await handler.Handle(
            StandardCmd(tour.Id, name: "Another Start", type: WaypointType.Start),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.InvalidType");
    }

    // ── Single-End invariant ───────────────────────────────────────────────────

    [Fact]
    public async Task Add_WhenEndAlreadyExists_Returns409InvalidType()
    {
        // PDF B1: exactly one End waypoint per tour.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existing = TestWaypointFactory.Create(tour.Id, name: "Tour End",
            waypointType: WaypointType.End);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, existing);

        var result = await handler.Handle(
            StandardCmd(tour.Id, name: "Another End", type: WaypointType.End),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.InvalidType");
    }

    // ── concurrency conflict ──────────────────────────────────────────────────

    [Fact]
    public async Task Add_ConcurrencyException_Returns409ConcurrencyConflict()
    {
        // PDF Critical Rule 11: inner DbUpdateConcurrencyException catch.
        var (handler, tourRepo, waypointRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);
        StubQuery(waypointRepo);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
           .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ConcurrencyConflict");
    }

    // ── Suspended tour allows (PDF B1.1) ─────────────────────────────────────

    [Fact]
    public async Task Add_SuspendedTour_AllowsButDoesNotBlockAdd()
    {
        // PDF B1.1: Suspended/Archived → log Warning but allow the mutation.
        var (handler, tourRepo, waypointRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateSuspended(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);
        StubQuery(waypointRepo);

        var result = await handler.Handle(StandardCmd(tour.Id), CancellationToken.None);

        // Handler must NOT return an error for Suspended (only logs warning).
        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
    }
}
