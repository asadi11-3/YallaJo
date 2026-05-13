using ContentTours.Application.Commands.Tour.ApproveTour;
using ContentTours.Application.Commands.Tour.ReinstateTour;
using ContentTours.Application.Commands.Tour.RejectTour;
using ContentTours.Application.Commands.Tour.SuspendTour;
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
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Approve / Reject / Suspend / Reinstate handlers + concurrency conflict on Approve.
/// </summary>
public sealed class AdminLifecycleCommandHandlerTests
{
    [Fact]
    public async Task Approve_PendingTransitionsToApprovedAndStampsApprover()
    {
        var repo = Substitute.For<ITourRepository>();
        var uow  = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<ApproveTourCommandHandler>>();

        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreatePending();
        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var handler = new ApproveTourCommandHandler(repo, uow, cache, currentUser, logger);

        var result = await handler.Handle(new ApproveTourCommand(tour.Id, tour.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Approved);
        tour.ApprovedByUserId.Should().Be(admin);
    }

    [Fact]
    public async Task Approve_SecondApprovalWithStaleRowVersionReturnsConcurrencyConflict()
    {
        // Two concurrent admins. The first SaveChangesAsync succeeds; the second
        // sees the same original RowVersion but EF re-checks at write time and
        // throws DbUpdateConcurrencyException — handler must translate that to 409.
        var repo = Substitute.For<ITourRepository>();
        var uow  = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<ApproveTourCommandHandler>>();

        var tour = TestTourFactory.CreatePending();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateConcurrencyException());

        var handler = new ApproveTourCommandHandler(repo, uow, cache, currentUser, logger);

        var result = await handler.Handle(new ApproveTourCommand(tour.Id, tour.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.ConcurrencyConflict");
    }

    [Fact]
    public async Task Reject_BlankReasonHandlerReturnsTourReasonRequired()
    {
        // The validator runs in the MediatR pipeline; we test the handler-level fallback
        // here because the handler must still defend itself if a caller bypasses validation.
        var repo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<RejectTourCommandHandler>>();

        var tour = TestTourFactory.CreatePending();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var handler = new RejectTourCommandHandler(repo, uow, cache, currentUser, logger);

        var result = await handler.Handle(
            new RejectTourCommand(tour.Id, tour.RowVersion, Reason: " "),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.ReasonRequired");
    }

    [Fact]
    public async Task Reject_PendingTransitionsToRejectedWithReasonAndAuditFields()
    {
        var repo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<RejectTourCommandHandler>>();

        var admin = Guid.NewGuid();
        var tour = TestTourFactory.CreatePending();
        currentUser.UserId.Returns(admin);
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var handler = new RejectTourCommandHandler(repo, uow, cache, currentUser, logger);

        var result = await handler.Handle(
            new RejectTourCommand(tour.Id, tour.RowVersion, Reason: "Photos blurry."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Rejected);
        tour.RejectionReason.Should().Be("Photos blurry.");
        tour.RejectedByUserId.Should().Be(admin);
        tour.RejectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Suspend_ApprovedTransitionsToSuspendedAndStoresReason()
    {
        var repo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<SuspendTourCommandHandler>>();

        var tour = TestTourFactory.CreateApproved();
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var handler = new SuspendTourCommandHandler(repo, uow, cache, currentUser, logger);

        var result = await handler.Handle(
            new SuspendTourCommand(tour.Id, tour.RowVersion, Reason: "Compliance review."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Suspended);
        tour.SuspensionReason.Should().Be("Compliance review.");
        tour.SuspendedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Reinstate_SuspendedTransitionsBackToApprovedAndClearsSuspension()
    {
        var repo = Substitute.For<ITourRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<ReinstateTourCommandHandler>>();

        var tour = TestTourFactory.CreateSuspended();
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var handler = new ReinstateTourCommandHandler(repo, uow, cache, logger);

        var result = await handler.Handle(
            new ReinstateTourCommand(tour.Id, tour.RowVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Approved);
        tour.SuspensionReason.Should().BeNull();
        tour.ReinstatedAt.Should().NotBeNull();
    }
}
