using ContentTours.Application.Commands.Tour.ToggleTourFeatured;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Tests for the admin-only ToggleTourFeatured command (PDF Task-3 §B8).
/// </summary>
public sealed class ToggleTourFeaturedHandlerTests
{
    private static (
        ToggleTourFeaturedCommandHandler Handler,
        ITourRepository Repo,
        IContentToursEventUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursEventUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<ToggleTourFeaturedCommandHandler>>();

        var handler = new ToggleTourFeaturedCommandHandler(repo, uow, cache, currentUser, logger);
        return (handler, repo, uow, currentUser);
    }

    [Fact]
    public async Task Toggle_OnApprovedTour_Succeeds_AndDispatchesViaEventUoW()
    {
        var (handler, repo, uow, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved();

        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(tour.Id, IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.IsFeatured.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Toggle_OnDraftTour_Returns409CannotFeatureNonApproved()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft();

        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(tour.Id, IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.CannotFeatureNonApproved");
        tour.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task Toggle_OnSuspendedTour_Returns409CannotFeatureNonApproved()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreateSuspended();

        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(tour.Id, IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
    }

    [Fact]
    public async Task Toggle_TourNotFound_Returns404TourNotFound()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ContentTours.Domain.Entities.Tour?)null);

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(Guid.NewGuid(), IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotFound");
    }

    [Fact]
    public async Task Toggle_Idempotent_DoesNotChangeStateWhenAlreadyAtTargetValue()
    {
        var (handler, repo, uow, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved();
        tour.SetFeatured(true, admin);   // already featured
        tour.ClearDomainEvents();

        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(tour.Id, IsFeatured: true),  // toggle to same value
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.IsFeatured.Should().BeTrue();
        // No domain event should have been raised by the idempotent SetFeatured call.
        tour.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Toggle_ConcurrencyConflict_Returns409TourConcurrencyConflict()
    {
        var (handler, repo, uow, currentUser) = BuildSubject();
        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved();

        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(
            new ToggleTourFeaturedCommand(tour.Id, IsFeatured: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.ConcurrencyConflict");
    }
}
