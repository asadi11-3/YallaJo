using ContentTours.Application.Commands.Tour.DeleteTour;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Tests.Unit;

public sealed class DeleteTourCommandHandlerTests
{
    private static (
        DeleteTourCommandHandler Handler,
        ITourRepository Repo,
        IContentToursOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<ITourRepository>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var uow = Substitute.For<IContentToursEventUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<DeleteTourCommandHandler>>();

        var handler = new DeleteTourCommandHandler(repo, outbox, uow, cache, currentUser, logger);
        return (handler, repo, outbox, currentUser);
    }

    [Fact]
    public async Task ApprovedTourWithFutureSchedulesIsBlocked409()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateApproved(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        repo.HasFutureSchedulesAsync(tour.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new DeleteTourCommand(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.DeleteBlocked");
    }

    [Fact]
    public async Task TourWithBookingsIsBlocked409()
    {
        // BookingCount > 0 path. We need to inject a booking count without exposing
        // a public setter, so we use the aggregate's UpdateBookingCount(delta).
        var (handler, repo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);
        tour.UpdateBookingCount(3);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new DeleteTourCommand(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.DeleteBlocked");
    }

    [Fact]
    public async Task DraftWithNoBookingsIsSoftDeletedAndStagesIntegrationEvent()
    {
        var (handler, repo, outbox, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new DeleteTourCommand(tour.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.IsDeleted.Should().BeTrue();
        outbox.Received(1).Enqueue(Arg.Is<IIntegrationEvent>(e => e is TourDeletedIntegrationEvent));
    }
}
