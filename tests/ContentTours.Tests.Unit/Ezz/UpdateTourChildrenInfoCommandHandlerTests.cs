using ContentTours.Application.Caching;
using ContentTours.Application.Commands.ChildrenInfo.Update;
using ContentTours.Application.Interfaces;
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
/// Behaviour tests for <see cref="UpdateTourChildrenInfoCommandHandler"/>.
/// Mirrors the scenario-oriented style used in TourGuideAssignTests.
/// </summary>
public sealed class UpdateTourChildrenInfoCommandHandlerTests
{
    // ── subject factory ───────────────────────────────────────────────────────

    private static (
        UpdateTourChildrenInfoCommandHandler Handler,
        ITourRepository TourRepo,
        IContentToursUnitOfWork Uow,
        HybridCache Cache,
        ICurrentUser CurrentUser)
        Build()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateTourChildrenInfoCommandHandler>>();

        var handler = new UpdateTourChildrenInfoCommandHandler(tourRepo, uow, cache, currentUser, logger);
        return (handler, tourRepo, uow, cache, currentUser);
    }

    private static UpdateTourChildrenInfoCommand Cmd(
        Guid tourId,
        bool allowsChildren = true,
        int? minChildAge = 5,
        int? maxChildAge = 10,
        string? childFacilities = "Stroller,PlayArea")
        => new(tourId, allowsChildren, minChildAge, maxChildAge, childFacilities);

    private static void AsOwner(ICurrentUser currentUser, Guid ownerId)
    {
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
    }

    // ── Tour.NotFound ────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_TourNotFound_Returns404()
    {
        var (handler, tourRepo, _, _, currentUser) = Build();

        currentUser.UserId.Returns(Guid.NewGuid());
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ContentTours.Domain.Entities.Tour?)null);

        var result = await handler.Handle(Cmd(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    // ── ownership / admin-tier ───────────────────────────────────────────────

    [Fact]
    public async Task Update_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, _, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(Cmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task Update_AdminTierRoles_CanUpdateEvenWhenNotOwner(string role)
    {
        var (handler, tourRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var elevated = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(elevated);
        currentUser.Roles.Returns(new[] { role });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(Cmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── parser failure / Tour.UnknownChildFacility ───────────────────────────

    [Fact]
    public async Task Update_UnknownFacility_Returns400TourUnknownChildFacility()
    {
        var (handler, tourRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        AsOwner(currentUser, owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            Cmd(tour.Id, childFacilities: "Stroller,NotAFacility"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.UnknownChildFacility");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── happy path / aggregate updated ───────────────────────────────────────

    [Fact]
    public async Task Update_HappyPath_UpdatesAggregateAndSaves()
    {
        var (handler, tourRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        AsOwner(currentUser, owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            Cmd(tour.Id, allowsChildren: true, minChildAge: 6, maxChildAge: 12, childFacilities: "PlayArea,Stroller,PlayArea"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.AllowsChildren.Should().BeTrue();
        tour.MinChildAge.Should().Be(6);
        tour.MaxChildAge.Should().Be(12);
        tour.IsChildFriendly.Should().BeTrue();
        tour.ChildFacilities.Select(x => x.Facility).Should().Equal(
            ContentTours.Domain.Enums.ChildFacility.Stroller,
            ContentTours.Domain.Enums.ChildFacility.PlayArea);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_HappyPath_InvalidatesChildrenInfoAndTourTags()
    {
        var (handler, tourRepo, _, cache, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        AsOwner(currentUser, owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        await handler.Handle(Cmd(tour.Id), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            TourChildrenInfoCacheKeys.TagForTour(tour.Id),
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagForTour(tour.Id),
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagToursList,
            Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagToursSearch,
            Arg.Any<CancellationToken>());
    }

    // ── concurrency conflict ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_SaveConcurrencyException_Returns409TourConcurrencyConflict()
    {
        var (handler, tourRepo, uow, _, currentUser) = Build();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        AsOwner(currentUser, owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(Cmd(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.ConcurrencyConflict");
    }

    // ── cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WhenCancellationRequested_ReturnsOutcomeCanceled()
    {
        var (handler, tourRepo, _, _, currentUser) = Build();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        currentUser.UserId.Returns(Guid.NewGuid());
        tourRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(_ => Task.FromCanceled<ContentTours.Domain.Entities.Tour?>(cts.Token));

        var result = await handler.Handle(Cmd(Guid.NewGuid()), cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Canceled);
        result.Errors.Should().ContainSingle(e => e.Code == "Request.Cancelled");
    }
}
