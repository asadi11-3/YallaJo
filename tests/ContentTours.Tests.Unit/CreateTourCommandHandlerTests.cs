using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Commands.Tour.CreateTour;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using Accounts.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

public sealed class CreateTourCommandHandlerTests
{
    private static (
        CreateTourCommandHandler Handler,
        ITourRepository Repo,
        IPlaceExistenceService PlaceExists,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<ITourRepository>();
        var place = Substitute.For<IPlaceExistenceService>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateTourCommandHandler>>();
        var providerStatus = Substitute.For<IProviderStatusService>();
        providerStatus.IsApprovedProviderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateTourCommandHandler(repo, place, providerStatus, uow, cache, currentUser, logger);
        return (handler, repo, place, uow, currentUser);
    }

    private static CreateTourCommand ValidCommand(string slug = "petra-day-tour", Guid? placeId = null) =>
        new(
            Name:                    "Petra Day Tour",
            Slug:                    slug,
            Difficulty:              Difficulty.Easy,
            DurationMinutes:         480,
            MaxGroupSize:            20,
            BasePrice:               100m,
            Currency:                "JOD",
            Latitude:                30.32m,
            Longitude:               35.45m,
            PlaceId:                 placeId);

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, currentUser) = BuildSubject();
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    [Fact]
    public async Task SuccessReturnsCreatedAndDraftStatus()
    {
        var (handler, repo, place, uow, currentUser) = BuildSubject();
        var userId = Guid.NewGuid();
        currentUser.UserId.Returns(userId);
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.Slug.Should().Be("petra-day-tour");

        await repo.Received(1).AddAsync(
            Arg.Is<Tour>(t => t.Status == TourStatus.Draft && t.CreatedByUserId == userId),
            Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateSlugReturnsTourSlugConflict()
    {
        var (handler, repo, _, _, currentUser) = BuildSubject();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.IsSlugReservedAsync("petra-day-tour", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.SlugConflict");
    }

    [Fact]
    public async Task ReservedSoftDeletedSlugReturnsTourSlugConflict()
    {
        // Slug reservation grace is enforced by ITourRepository.IsSlugReservedAsync.
        // This test asserts the handler honours it (returns SlugConflict / 409) when
        // the repository says the slug is reserved by a soft-deleted tour within 30 days.
        var (handler, repo, _, _, currentUser) = BuildSubject();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true); // simulating soft-deleted tour within the 30-day window.

        var result = await handler.Handle(ValidCommand("recently-deleted-slug"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.SlugConflict");
    }

    [Fact]
    public async Task MissingPlaceReturnsTourPlaceNotFound422()
    {
        var (handler, repo, place, _, currentUser) = BuildSubject();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        place.GetStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(PlaceExistenceStatus.NotFound);

        var result = await handler.Handle(ValidCommand(placeId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.PlaceNotFound");
    }

    [Fact]
    public async Task DeletedPlaceReturnsTourPlaceDeleted422()
    {
        var (handler, repo, place, _, currentUser) = BuildSubject();
        currentUser.UserId.Returns(Guid.NewGuid());
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        place.GetStatusAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(PlaceExistenceStatus.Deleted);

        var result = await handler.Handle(ValidCommand(placeId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.PlaceDeleted");
    }
}
