using ContentPlaces.Contracts.Places;
using ContentTours.Application.Commands.Tour.UpdateTour;
using ContentTours.Application.Interfaces;
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

namespace ContentTours.Tests.Unit;

public sealed class UpdateTourCommandHandlerTests
{
    private static (
        UpdateTourCommandHandler Handler,
        ITourRepository Repo,
        IPlaceExistenceService PlaceExists,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<ITourRepository>();
        var place = Substitute.For<IPlaceExistenceService>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateTourCommandHandler>>();

        var handler = new UpdateTourCommandHandler(repo, place, uow, cache, currentUser, logger);
        return (handler, repo, place, currentUser);
    }

    private static UpdateTourCommand BuildCommand(Tour tour, byte[]? rowVersion = null) =>
        new(
            Id:               tour.Id,
            RowVersion:       rowVersion ?? tour.RowVersion,
            Name:             tour.Name,
            Slug:             tour.Slug,
            Difficulty:       tour.Difficulty,
            DurationMinutes:  tour.DurationMinutes,
            MaxGroupSize:     tour.MaxGroupSize,
            BasePrice:        tour.BasePrice.Amount,
            Currency:         tour.Currency,
            Latitude:         tour.Location.Latitude,
            Longitude:        tour.Location.Longitude);

    [Fact]
    public async Task NonOwnerStandardUserReturnsTourNotOwner403()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var owner  = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(tour);

        var result = await handler.Handle(BuildCommand(tour), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task UpdateRejectedTourResetsToDraftAndClearsRejectionAudit()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateRejected(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(tour);
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(tour), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Status.Should().Be(TourStatus.Draft);
        tour.RejectionReason.Should().BeNull();
        tour.RejectedAt.Should().BeNull();
        tour.RejectedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task RowVersionMismatchReturnsConcurrencyConflict()
    {
        var (handler, repo, _, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(tour);

        var staleVersion = new byte[] { 0xFF, 0x00 };
        var result = await handler.Handle(BuildCommand(tour, rowVersion: staleVersion), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.ConcurrencyConflict");
    }
}
