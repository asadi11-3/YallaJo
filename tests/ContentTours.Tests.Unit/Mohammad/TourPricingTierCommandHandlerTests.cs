using System.Linq.Expressions;
using ContentTours.Application.Commands.TourPricingTier.CreateTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
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
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Behaviour tests for TourPricingTier handlers and the AdultTierGuard rule.
/// Drives each handler through NSubstitute mocks of <see cref="ITourPricingTierRepository"/>,
/// <see cref="ITourRepository"/>, <see cref="IContentToursOutboxWriter"/>,
/// <see cref="IContentToursUnitOfWork"/>, <see cref="ICurrentUser"/>.
/// </summary>
public sealed class TourPricingTierCommandHandlerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static (
        CreateTourPricingTierCommandHandler Handler,
        ITourRepository TourRepo,
        ITourPricingTierRepository TierRepo,
        IContentToursUnitOfWork Uow,
        IContentToursOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildCreateSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var tierRepo = Substitute.For<ITourPricingTierRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var translationOrchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        var tierTranslationRepo = Substitute.For<ITourPricingTierTranslationRepository>();
        var logger = Substitute.For<ILogger<CreateTourPricingTierCommandHandler>>();

        var handler = new CreateTourPricingTierCommandHandler(
            tourRepo, tierRepo, uow, outbox, cache, currentUser,
            translationOrchestrator, tierTranslationRepo, logger);
        return (handler, tourRepo, tierRepo, uow, outbox, currentUser);
    }

    private static (
        UpdateTourPricingTierCommandHandler Handler,
        ITourRepository TourRepo,
        ITourPricingTierRepository TierRepo,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildUpdateSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var tierRepo = Substitute.For<ITourPricingTierRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateTourPricingTierCommandHandler>>();

        var handler = new UpdateTourPricingTierCommandHandler(
            tourRepo, tierRepo, uow, outbox, cache, currentUser, logger);
        return (handler, tourRepo, tierRepo, uow, currentUser);
    }

    private static (
        DeleteTourPricingTierCommandHandler Handler,
        ITourRepository TourRepo,
        ITourPricingTierRepository TierRepo,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildDeleteSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var tierRepo = Substitute.For<ITourPricingTierRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<DeleteTourPricingTierCommandHandler>>();

        var handler = new DeleteTourPricingTierCommandHandler(
            tourRepo, tierRepo, uow, outbox, cache, currentUser, logger);
        return (handler, tourRepo, tierRepo, uow, currentUser);
    }

    private static void StubGetAll(ITourPricingTierRepository repo, params TourPricingTier[] tiers)
        => repo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourPricingTier, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourPricingTier>, IQueryable<TourPricingTier>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourPricingTier>, IOrderedQueryable<TourPricingTier>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
            .Returns(tiers.ToList());

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_AdultTierByOwner_Succeeds_AndStagesOutboxEvent()
    {
        var (handler, tourRepo, tierRepo, _, outbox, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(tierRepo); // no existing tiers

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId: tour.Id,
            Name: "Adult",
            Description: null,
            Price: 50m,
            Currency: "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1,
            MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await tierRepo.Received(1).AddAsync(
            Arg.Is<TourPricingTier>(t => t.TourId == tour.Id && t.IsAdult),
            Arg.Any<CancellationToken>());
        outbox.Received(1).Enqueue(Arg.Is<IIntegrationEvent>(e => e is TourPricingTierChangedIntegrationEvent));
    }

    [Fact]
    public async Task Create_DuplicateNameCaseInsensitive_Returns409NameConflict()
    {
        var (handler, tourRepo, tierRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(tierRepo, TestPricingTierFactory.Create(tour.Id, name: "Adult"));

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId: tour.Id,
            Name: "adult",       // different casing — must still conflict
            Description: null,
            Price: 50m, Currency: "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1, MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPricingTier.NameConflict");
    }

    [Fact]
    public async Task Create_CurrencyMismatch_Returns400CurrencyMismatch()
    {
        var (handler, tourRepo, tierRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);  // tour currency JOD

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(tierRepo);

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId: tour.Id, Name: "Adult", Description: null,
            Price: 50m, Currency: "USD",   // mismatch
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1, MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPricingTier.CurrencyMismatch");
    }

    [Fact]
    public async Task Create_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, tierRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(tierRepo);

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId: tour.Id, Name: "Adult", Description: null,
            Price: 50m, Currency: "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1, MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Create_ConcurrencyConflict_Returns409TourPricingTierConcurrencyConflict()
    {
        var (handler, tourRepo, tierRepo, uow, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(tierRepo);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId: tour.Id, Name: "Adult", Description: null,
            Price: 50m, Currency: "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1, MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPricingTier.ConcurrencyConflict");
    }

    // ── Delete — Adult-tier guard ─────────────────────────────────────────────

    [Fact]
    public async Task Delete_LastAdultTier_OnApprovedTour_Returns409AdultTierRequired()
    {
        var (handler, tourRepo, tierRepo, _, currentUser) = BuildDeleteSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var adult = TestPricingTierFactory.Create(tour.Id, name: "Adult", participantType: ParticipantType.Adult);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        tierRepo.GetByIdAsync(adult.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(adult);
        StubGetAll(tierRepo, adult); // only one Adult tier exists

        var result = await handler.Handle(
            new DeleteTourPricingTierCommand(tour.Id, adult.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPricingTier.AdultTierRequired");
        tierRepo.DidNotReceive().Remove(Arg.Any<TourPricingTier>());
    }

    [Fact]
    public async Task Delete_LastAdultTier_OnDraftTour_Succeeds()
    {
        var (handler, tourRepo, tierRepo, _, currentUser) = BuildDeleteSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner); // editable
        var adult = TestPricingTierFactory.Create(tour.Id, name: "Adult", participantType: ParticipantType.Adult);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        tierRepo.GetByIdAsync(adult.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(adult);
        StubGetAll(tierRepo, adult);

        var result = await handler.Handle(
            new DeleteTourPricingTierCommand(tour.Id, adult.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tierRepo.Received(1).Remove(adult);
    }

    [Fact]
    public async Task Delete_AdultTierWhenAnotherActiveAdultExists_Succeeds()
    {
        var (handler, tourRepo, tierRepo, _, currentUser) = BuildDeleteSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var adult1 = TestPricingTierFactory.Create(tour.Id, name: "Adult Standard", participantType: ParticipantType.Adult);
        var adult2 = TestPricingTierFactory.Create(tour.Id, name: "Adult Group", participantType: ParticipantType.Adult);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        tierRepo.GetByIdAsync(adult1.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(adult1);
        StubGetAll(tierRepo, adult1, adult2); // two active Adult tiers exist

        var result = await handler.Handle(
            new DeleteTourPricingTierCommand(tour.Id, adult1.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tierRepo.Received(1).Remove(adult1);
    }

    // ── Update — deactivate-last-Adult guard ──────────────────────────────────

    [Fact]
    public async Task Update_DeactivatingLastAdultTier_OnApprovedTour_Returns409AdultTierRequired()
    {
        var (handler, tourRepo, tierRepo, _, currentUser) = BuildUpdateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var adult = TestPricingTierFactory.Create(tour.Id, name: "Adult", participantType: ParticipantType.Adult);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        tierRepo.GetByIdAsync(adult.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(adult);
        StubGetAll(tierRepo, adult); // only one Adult tier exists

        var result = await handler.Handle(new UpdateTourPricingTierCommand(
            TourId: tour.Id, TierId: adult.Id,
            Name: "Adult", Description: null,
            Price: 50m, Currency: "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1, MaxParticipants: null,
            IsActive: false),   // deactivating
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourPricingTier.AdultTierRequired");
    }

    // ── Domain — participant range ────────────────────────────────────────────

    [Fact]
    public void DomainEntity_RejectsZeroMinParticipants()
    {
        // PDF B1.5: MinParticipants ≥ 1, MaxParticipants > MinParticipants.
        // Domain throws ArgumentOutOfRangeException — production validator surfaces a 400.
        var act = () => TestPricingTierFactory.Create(Guid.NewGuid(), min: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DomainEntity_RejectsMaxLessOrEqualToMin()
    {
        var act = () => TestPricingTierFactory.Create(Guid.NewGuid(), min: 5, max: 5);
        act.Should().Throw<ArgumentException>();
    }
}
