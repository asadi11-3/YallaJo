using System.Linq.Expressions;
using ContentTours.Application.Commands.TourGuides.Assign;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Entities;
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
using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Behaviour tests for <see cref="AssignTourGuideCommandHandler"/>.
/// Covers PDF Critical Rules 2, 5, 6, 7, 9, 11, 12 and Phase C PDF B5 domain rules.
/// </summary>
public sealed class TourGuideAssignTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        AssignTourGuideCommandHandler Handler,
        ITourRepository TourRepo,
        ITourTourGuideRepository GuideRepo,
        IContentToursUnitOfWork Uow,
        IContentToursOutboxWriter Outbox,
        IUserRoleChecker RoleChecker,
        ICurrentUser CurrentUser)
        Build()
    {
        var tourRepo    = Substitute.For<ITourRepository>();
        var guideRepo   = Substitute.For<ITourTourGuideRepository>();
        var uow         = Substitute.For<IContentToursUnitOfWork>();
        var outbox      = Substitute.For<IContentToursOutboxWriter>();
        var roleChecker = Substitute.For<IUserRoleChecker>();
        var cache       = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger      = Substitute.For<ILogger<AssignTourGuideCommandHandler>>();

        // Default: target user HAS the TourGuide role (happy-path default)
        roleChecker.HasRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(true);

        var handler = new AssignTourGuideCommandHandler(
            tourRepo, guideRepo, uow, outbox, roleChecker, cache, currentUser, logger);
        return (handler, tourRepo, guideRepo, uow, outbox, roleChecker, currentUser);
    }

    // ── stub helpers ──────────────────────────────────────────────────────────

    private static void StubGetAll(ITourTourGuideRepository repo, params TourTourGuide[] guides)
        => repo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourTourGuide, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourTourGuide>, IQueryable<TourTourGuide>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourTourGuide>, IOrderedQueryable<TourTourGuide>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
           .Returns(guides.ToList());

    // ── auto-promote on first assignment ──────────────────────────────────────

    [Fact]
    public async Task Assign_FirstAssignment_IsPrimaryFalse_AutoPromotesToPrimary()
    {
        // PDF B5: first guide is auto-promoted to primary regardless of request.IsPrimary.
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner  = Guid.NewGuid();
        var tour   = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo); // no existing assignments

        var cmd = new AssignTourGuideCommand(tour.Id, guideId, IsPrimary: false);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await guideRepo.Received(1).AddAsync(
            Arg.Is<TourTourGuide>(g => g.IsPrimary == true && g.TourGuideId == guideId),
            Arg.Any<CancellationToken>());
    }

    // ── explicit primary demotes existing ─────────────────────────────────────

    [Fact]
    public async Task Assign_ExplicitPrimary_DemotesExistingPrimary()
    {
        // When IsPrimary: true and another guide is already primary,
        // the existing primary must be demoted (SetAsNonPrimary called).
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner          = Guid.NewGuid();
        var tour           = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existingGuideId = Guid.NewGuid();
        var newGuideId      = Guid.NewGuid();

        var existingPrimary = TourTourGuide.Create(tour.Id, existingGuideId, isPrimary: true);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, existingPrimary);

        var cmd = new AssignTourGuideCommand(tour.Id, newGuideId, IsPrimary: true);
        await handler.Handle(cmd, CancellationToken.None);

        // Existing primary was demoted in-memory by SetAsNonPrimary()
        existingPrimary.IsPrimary.Should().BeFalse();
        // New guide was added as primary
        await guideRepo.Received(1).AddAsync(
            Arg.Is<TourTourGuide>(g => g.TourGuideId == newGuideId && g.IsPrimary == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assign_SecondAssignment_NotPrimary_DoesNotDemoteExisting()
    {
        // Assigning a non-primary second guide must not touch the existing primary.
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner            = Guid.NewGuid();
        var tour             = TestTourFactory.CreateApproved(createdByUserId: owner);
        var existingGuideId   = Guid.NewGuid();
        var newGuideId        = Guid.NewGuid();
        var existingPrimary   = TourTourGuide.Create(tour.Id, existingGuideId, isPrimary: true);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, existingPrimary);

        var cmd = new AssignTourGuideCommand(tour.Id, newGuideId, IsPrimary: false);
        await handler.Handle(cmd, CancellationToken.None);

        // Existing primary NOT demoted
        existingPrimary.IsPrimary.Should().BeTrue();
        await guideRepo.Received(1).AddAsync(
            Arg.Is<TourTourGuide>(g => g.TourGuideId == newGuideId && g.IsPrimary == false),
            Arg.Any<CancellationToken>());
    }

    // ── duplicate-assignment guard ────────────────────────────────────────────

    [Fact]
    public async Task Assign_DuplicateGuide_Returns409AlreadyAssigned()
    {
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner   = Guid.NewGuid();
        var tour    = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();
        var existing = TourTourGuide.Create(tour.Id, guideId, isPrimary: false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, existing);

        var result = await handler.Handle(
            new AssignTourGuideCommand(tour.Id, guideId, IsPrimary: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourTourGuide.AlreadyAssigned");
        await guideRepo.DidNotReceive().AddAsync(Arg.Any<TourTourGuide>(), Arg.Any<CancellationToken>());
    }

    // ── role-check failure ────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_UserNotGuide_Returns422NotAGuide()
    {
        // IUserRoleChecker.HasRoleAsync returns false → user lacks "TourGuide" role.
        var (handler, tourRepo, guideRepo, _, _, roleChecker, currentUser) = Build();
        var owner   = Guid.NewGuid();
        var tour    = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();

        roleChecker.HasRoleAsync(guideId, Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        var result = await handler.Handle(
            new AssignTourGuideCommand(tour.Id, guideId, IsPrimary: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourTourGuide.NotAGuide");
    }

    // ── Tour.NotFound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, _, _, _) = Build();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
                .Returns((Domain.Entities.Tour?)null);

        var result = await handler.Handle(
            new AssignTourGuideCommand(Guid.NewGuid(), Guid.NewGuid(), false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ownership gate ────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner    = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour     = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        var result = await handler.Handle(
            new AssignTourGuideCommand(tour.Id, Guid.NewGuid(), false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Assign_AdminNotOwner_Succeeds()
    {
        var (handler, tourRepo, guideRepo, _, _, _, currentUser) = Build();
        var owner   = Guid.NewGuid();
        var admin   = Guid.NewGuid();
        var tour    = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();

        currentUser.UserId.Returns(admin);
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        var result = await handler.Handle(
            new AssignTourGuideCommand(tour.Id, guideId, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── outbox enqueue ────────────────────────────────────────────────────────

    [Fact]
    public async Task Assign_HappyPath_OutboxEnqueuesIntegrationEvent()
    {
        var (handler, tourRepo, guideRepo, _, outbox, _, currentUser) = Build();
        var owner   = Guid.NewGuid();
        var tour    = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        await handler.Handle(
            new AssignTourGuideCommand(tour.Id, guideId, false),
            CancellationToken.None);

        outbox.Received(1).Enqueue(
            Arg.Is<IIntegrationEvent>(e => e is TourGuideAssignedIntegrationEvent));
    }

    // ── concurrency conflict ──────────────────────────────────────────────────

    [Fact]
    public async Task Assign_ConcurrencyException_Returns409ConcurrencyConflict()
    {
        var (handler, tourRepo, guideRepo, uow, _, _, currentUser) = Build();
        var owner   = Guid.NewGuid();
        var tour    = TestTourFactory.CreateApproved(createdByUserId: owner);
        var guideId = Guid.NewGuid();

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
           .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(
            new AssignTourGuideCommand(tour.Id, guideId, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourTourGuide.ConcurrencyConflict");
    }
}
