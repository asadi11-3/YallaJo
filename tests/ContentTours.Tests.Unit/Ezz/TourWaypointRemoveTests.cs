using System.Linq.Expressions;
using ContentTours.Application.Commands.TourWaypoints.RemoveTourWaypoint;
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
/// Behaviour tests for <see cref="RemoveTourWaypointCommandHandler"/>.
/// Covers delete + SortOrder re-compaction, ownership gate,
/// TourWaypoint.NotFound, and concurrency conflict.
/// </summary>
public sealed class TourWaypointRemoveTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        RemoveTourWaypointCommandHandler Handler,
        ITourRepository TourRepo,
        ITourWaypointRepository WaypointRepo,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser)
        Build()
    {
        var tourRepo     = Substitute.For<ITourRepository>();
        var waypointRepo = Substitute.For<ITourWaypointRepository>();
        var uow          = Substitute.For<IContentToursUnitOfWork>();
        var cache        = Substitute.For<HybridCache>();
        var currentUser  = Substitute.For<ICurrentUser>();
        var logger       = Substitute.For<ILogger<RemoveTourWaypointCommandHandler>>();

        var handler = new RemoveTourWaypointCommandHandler(
            tourRepo, waypointRepo, uow, cache, currentUser, logger);
        return (handler, tourRepo, waypointRepo, uow, currentUser);
    }

    // ── stub helpers ──────────────────────────────────────────────────────────

    private static void StubGetAll(
        ITourWaypointRepository repo,
        params TourWaypoint[] waypoints)
        => repo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourWaypoint, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourWaypoint>, IQueryable<TourWaypoint>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourWaypoint>, IOrderedQueryable<TourWaypoint>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
           .Returns(waypoints.ToList());

    // ── happy path: delete + re-compact ──────────────────────────────────────

    [Fact]
    public async Task Remove_MiddleWaypoint_RecompactsRemainingRows()
    {
        // Setup: 3 waypoints — Start(0), Stop(1), End(2).
        // Delete Stop(1) → Start stays 0, End shifts from 2 → 1.
        var (handler, tourRepo, waypointRepo, uow, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wp0 = TestWaypointFactory.Create(tour.Id, "Start",  WaypointType.Start, sortOrder: 0);
        var wp1 = TestWaypointFactory.Create(tour.Id, "Middle", WaypointType.Stop,  sortOrder: 1);
        var wp2 = TestWaypointFactory.Create(tour.Id, "End",    WaypointType.End,   sortOrder: 2);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp0, wp1, wp2);

        // Act
        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, wp1.Id),
            CancellationToken.None);

        // Assert: success, correct entity removed, SortOrder re-compact applied.
        result.IsSuccess.Should().BeTrue();
        waypointRepo.Received(1).Remove(wp1);
        wp0.SortOrder.Should().Be(0); // below deleted SortOrder — unchanged
        wp2.SortOrder.Should().Be(1); // above deleted SortOrder — decremented
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_OnlyWaypoint_DeletesWithNoRecompact()
    {
        // Deleting the sole waypoint — no re-compaction needed.
        var (handler, tourRepo, waypointRepo, uow, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);
        var wp    = TestWaypointFactory.Create(tour.Id, "Only One", WaypointType.Start, sortOrder: 0);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp);

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, wp.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        waypointRepo.Received(1).Remove(wp);
        // No SetSortOrder calls on other rows (there are none).
        wp.SortOrder.Should().Be(0); // the deleted one is unchanged (SortOrder mutated only on survivors)
    }

    [Fact]
    public async Task Remove_LastWaypoint_RecompactsOnlyPrecedingRows()
    {
        // Delete the last (highest SortOrder) waypoint.
        // All preceding rows have SortOrder ≤ deleted — none shift.
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wp0 = TestWaypointFactory.Create(tour.Id, "Start", WaypointType.Start, sortOrder: 0);
        var wp1 = TestWaypointFactory.Create(tour.Id, "End",   WaypointType.End,   sortOrder: 1);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp0, wp1);

        await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, wp1.Id), // delete End (SortOrder=1)
            CancellationToken.None);

        waypointRepo.Received(1).Remove(wp1);
        wp0.SortOrder.Should().Be(0); // not shifted — its SortOrder (0) < deleted (1)
    }

    // ── TourWaypoint.NotFound ─────────────────────────────────────────────────

    [Fact]
    public async Task Remove_WaypointNotFound_Returns404()
    {
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);
        var otherId  = Guid.NewGuid(); // not in the loaded list

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, TestWaypointFactory.Create(tour.Id));

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, otherId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.NotFound");
        waypointRepo.DidNotReceive().Remove(Arg.Any<TourWaypoint>());
    }

    // ── Tour.NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, _) = Build();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns((Domain.Entities.Tour?)null);

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ownership gate ────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo);

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
        waypointRepo.DidNotReceive().Remove(Arg.Any<TourWaypoint>());
    }

    [Fact]
    public async Task Remove_AdminNotOwner_Succeeds()
    {
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);
        var wp    = TestWaypointFactory.Create(tour.Id, "Stop", WaypointType.Stop, sortOrder: 0);

        currentUser.UserId.Returns(admin);
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp);

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, wp.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        waypointRepo.Received(1).Remove(wp);
    }

    // ── concurrency conflict ──────────────────────────────────────────────────

    [Fact]
    public async Task Remove_ConcurrencyException_Returns409ConcurrencyConflict()
    {
        var (handler, tourRepo, waypointRepo, uow, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);
        var wp    = TestWaypointFactory.Create(tour.Id, "Stop", WaypointType.Stop, sortOrder: 0);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
           .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(
            new RemoveTourWaypointCommand(tour.Id, wp.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ConcurrencyConflict");
    }
}
