using System.Linq.Expressions;
using ContentTours.Application.Commands.TourGuides.Unassign;
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

public sealed class TourGuideUnassignTests
{
    private static (
        UnassignTourGuideCommandHandler Handler,
        ITourRepository TourRepo,
        ITourTourGuideRepository GuideRepo,
        IContentToursUnitOfWork Uow,
        IContentToursOutboxWriter Outbox,
        ICurrentUser CurrentUser)
        Build()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var guideRepo = Substitute.For<ITourTourGuideRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UnassignTourGuideCommandHandler>>();

        var handler = new UnassignTourGuideCommandHandler(
            tourRepo, guideRepo, uow, outbox, cache, currentUser, logger);
        return (handler, tourRepo, guideRepo, uow, outbox, currentUser);
    }

    private static void StubGetAll(ITourTourGuideRepository repo, params TourTourGuide[] guides)
        => repo.GetAllAsync(
                filter: Arg.Any<Expression<Func<TourTourGuide, bool>>>(),
                include: Arg.Any<Func<IQueryable<TourTourGuide>, IQueryable<TourTourGuide>>?>(),
                orderBy: Arg.Any<Func<IQueryable<TourTourGuide>, IOrderedQueryable<TourTourGuide>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct: Arg.Any<CancellationToken>())
           .Returns(guides.ToList());

    [Fact]
    public async Task Unassign_PrimaryWithRemaining_PromotesSmallestGuideId()
    {
        var (handler, tourRepo, guideRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);

        var removedId = Guid.Parse("00000000-0000-0000-0000-000000000100");
        var promotedId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var otherId = Guid.Parse("00000000-0000-0000-0000-000000000200");

        var removed = TourTourGuide.Create(tour.Id, removedId, isPrimary: true);
        var promoted = TourTourGuide.Create(tour.Id, promotedId, isPrimary: false);
        var other = TourTourGuide.Create(tour.Id, otherId, isPrimary: false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, removed, promoted, other);

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, removedId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guideRepo.Received(1).Remove(removed);
        promoted.IsPrimary.Should().BeTrue();
        other.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Unassign_NonPrimary_DoesNotPromoteAnotherGuide()
    {
        var (handler, tourRepo, guideRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var primaryId = Guid.NewGuid();
        var removedId = Guid.NewGuid();

        var primary = TourTourGuide.Create(tour.Id, primaryId, isPrimary: true);
        var removed = TourTourGuide.Create(tour.Id, removedId, isPrimary: false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, primary, removed);

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, removedId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        primary.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Unassign_NotAssigned_Returns404TourTourGuideNotFound()
    {
        var (handler, tourRepo, guideRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "TourTourGuide.NotFound");
        guideRepo.DidNotReceive().Remove(Arg.Any<TourTourGuide>());
    }

    [Fact]
    public async Task Unassign_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, _, _) = Build();
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((Tour?)null);

        var result = await handler.Handle(new UnassignTourGuideCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    [Fact]
    public async Task Unassign_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, guideRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo);

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Unassign_AdminNotOwner_Succeeds()
    {
        var (handler, tourRepo, guideRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var guideId = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var assignment = TourTourGuide.Create(tour.Id, guideId, isPrimary: true);

        currentUser.UserId.Returns(admin);
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, assignment);

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, guideId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Unassign_HappyPath_EnqueuesUnassignedIntegrationEvent()
    {
        var (handler, tourRepo, guideRepo, _, outbox, currentUser) = Build();
        var owner = Guid.NewGuid();
        var guideId = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var assignment = TourTourGuide.Create(tour.Id, guideId, isPrimary: false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, assignment);

        await handler.Handle(new UnassignTourGuideCommand(tour.Id, guideId), CancellationToken.None);

        outbox.Received(1).Enqueue(Arg.Is<IIntegrationEvent>(e => e is TourGuideUnassignedIntegrationEvent));
    }

    [Fact]
    public async Task Unassign_ConcurrencyException_Returns409ConcurrencyConflict()
    {
        var (handler, tourRepo, guideRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var guideId = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);
        var assignment = TourTourGuide.Create(tour.Id, guideId, isPrimary: false);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(guideRepo, assignment);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(new UnassignTourGuideCommand(tour.Id, guideId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourTourGuide.ConcurrencyConflict");
    }
}
