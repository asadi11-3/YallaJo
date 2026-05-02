using System.Linq.Expressions;
using ContentTours.Application.Commands.TourWaypoints.ReorderTourWaypoints;
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
/// Behaviour tests for <see cref="ReorderTourWaypointsCommandHandler"/>.
/// Covers PDF B2 set-equality + duplicate guards, ownership gate, and
/// dense 0..N-1 sort-order reassignment.
/// </summary>
public sealed class TourWaypointReorderTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        ReorderTourWaypointsCommandHandler Handler,
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
        var logger       = Substitute.For<ILogger<ReorderTourWaypointsCommandHandler>>();

        var handler = new ReorderTourWaypointsCommandHandler(
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

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_HappyPath_ThreeWaypoints_ProducesDenseZeroToTwo()
    {
        // Arrange: 3 waypoints in original order [Start(0), Stop(1), End(2)].
        // Request: reverse the order → [End, Stop, Start].
        // Expected: End.SortOrder=0, Stop.SortOrder=1, Start.SortOrder=2.
        var (handler, tourRepo, waypointRepo, uow, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wp0 = TestWaypointFactory.Create(tour.Id, "Start Point",
            WaypointType.Start, sortOrder: 0);
        var wp1 = TestWaypointFactory.Create(tour.Id, "Middle Stop",
            WaypointType.Stop, sortOrder: 1);
        var wp2 = TestWaypointFactory.Create(tour.Id, "End Point",
            WaypointType.End, sortOrder: 2);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp0, wp1, wp2);

        var cmd = new ReorderTourWaypointsCommand(tour.Id,
            new List<Guid> { wp2.Id, wp1.Id, wp0.Id });

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wp2.SortOrder.Should().Be(0);
        wp1.SortOrder.Should().Be(1);
        wp0.SortOrder.Should().Be(2);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── duplicate ids guard (PDF B2) ──────────────────────────────────────────

    [Fact]
    public async Task Reorder_DuplicateIds_Returns422ReorderDuplicates()
    {
        // Duplicate check fires BEFORE loading waypoints.
        var (handler, tourRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);
        var someId = Guid.NewGuid();

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var cmd = new ReorderTourWaypointsCommand(tour.Id,
            new List<Guid> { someId, someId }); // same id twice

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ReorderDuplicates");
    }

    // ── set-equality guard (PDF B2) ───────────────────────────────────────────

    [Fact]
    public async Task Reorder_SetMismatch_OmitsOne_Returns422ReorderSetMismatch()
    {
        // Current = {A, B, C}; provided = {A, B} (C missing) → mismatch.
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wpA = TestWaypointFactory.Create(tour.Id, "A", WaypointType.Start, 0);
        var wpB = TestWaypointFactory.Create(tour.Id, "B", WaypointType.Stop,  1);
        var wpC = TestWaypointFactory.Create(tour.Id, "C", WaypointType.End,   2);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wpA, wpB, wpC);

        var cmd = new ReorderTourWaypointsCommand(tour.Id,
            new List<Guid> { wpA.Id, wpB.Id }); // C omitted

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ReorderSetMismatch");
    }

    [Fact]
    public async Task Reorder_SetMismatch_AddsUnknown_Returns422ReorderSetMismatch()
    {
        // Current = {A, B}; provided = {A, B, X} (X unknown) → mismatch.
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wpA = TestWaypointFactory.Create(tour.Id, "A", WaypointType.Start, 0);
        var wpB = TestWaypointFactory.Create(tour.Id, "B", WaypointType.End,   1);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wpA, wpB);

        var cmd = new ReorderTourWaypointsCommand(tour.Id,
            new List<Guid> { wpA.Id, wpB.Id, Guid.NewGuid() }); // unknown id added

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ReorderSetMismatch");
    }

    // ── Tour.NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, _) = Build();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns((Domain.Entities.Tour?)null);

        var result = await handler.Handle(
            new ReorderTourWaypointsCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ownership gate ────────────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_NonOwnerNonAdmin_Returns403TourNotOwner()
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
            new ReorderTourWaypointsCommand(tour.Id, new List<Guid>()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Reorder_AdminNotOwner_Succeeds()
    {
        var (handler, tourRepo, waypointRepo, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);

        var wp = TestWaypointFactory.Create(tour.Id, "Only Stop", WaypointType.Stop, 0);

        currentUser.UserId.Returns(admin);
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp);

        var result = await handler.Handle(
            new ReorderTourWaypointsCommand(tour.Id, new List<Guid> { wp.Id }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── concurrency conflict ──────────────────────────────────────────────────

    [Fact]
    public async Task Reorder_ConcurrencyException_Returns409ConcurrencyConflict()
    {
        var (handler, tourRepo, waypointRepo, uow, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour  = TestTourFactory.CreateApproved(createdByUserId: owner);
        var wp    = TestWaypointFactory.Create(tour.Id, "A", WaypointType.Stop, 0);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(waypointRepo, wp);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
           .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(
            new ReorderTourWaypointsCommand(tour.Id, new List<Guid> { wp.Id }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourWaypoint.ConcurrencyConflict");
    }

}
