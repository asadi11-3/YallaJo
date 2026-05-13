using ContentPlaces.Contracts.Places;
using ContentTours.Application.Caching;
using ContentTours.Application.Commands.Tour.UpdateTour;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

/// <summary>
/// P1-005 regression tests:
/// UpdateTourCommandHandler must invalidate slug-keyed cache entries for both the
/// old and the new slug after a successful save, and must NOT evict any cache on
/// Unauthorized / NotFound / Forbidden / Conflict outcomes.
/// </summary>
public sealed class UpdateTourSlugCacheInvalidationTests
{
    private static (
        UpdateTourCommandHandler Handler,
        ITourRepository Repo,
        IPlaceExistenceService PlaceExists,
        HybridCache Cache,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var repo = Substitute.For<ITourRepository>();
        var place = Substitute.For<IPlaceExistenceService>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateTourCommandHandler>>();

        var handler = new UpdateTourCommandHandler(repo, place, uow, cache, currentUser, logger);
        return (handler, repo, place, cache, currentUser);
    }

    private static UpdateTourCommand BuildCommand(Tour tour, string newSlug, byte[]? rowVersion = null) =>
        new(
            Id:               tour.Id,
            RowVersion:       rowVersion ?? tour.RowVersion,
            Name:             tour.Name,
            Slug:             newSlug,
            Difficulty:       tour.Difficulty,
            DurationMinutes:  tour.DurationMinutes,
            MaxGroupSize:     tour.MaxGroupSize,
            BasePrice:        tour.BasePrice.Amount,
            Currency:         tour.Currency,
            Latitude:         tour.Location.Latitude,
            Longitude:        tour.Location.Longitude);

    [Fact]
    public async Task SlugChange_AfterSave_EvictsOldAndNewSlugTags()
    {
        var (handler, repo, _, cache, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();

        // Build a tour with a known starting slug, captured BEFORE the handler runs.
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner, slug: "old-slug");
        var oldSlug = tour.Slug;
        const string newSlug = "brand-new-slug";

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(tour, newSlug), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tour.Slug.Should().Be(newSlug);

        // Old slug tag must be evicted — exactly once.
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagForTourSlug(oldSlug),
            Arg.Any<CancellationToken>());

        // New slug tag must also be evicted — exactly once — because slug changed.
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagForTourSlug(newSlug),
            Arg.Any<CancellationToken>());

        // Pre-existing eviction surface preserved.
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

    [Fact]
    public async Task SlugUnchanged_AfterSave_EvictsSlugTagOnce_NoDuplicate()
    {
        var (handler, repo, _, cache, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();

        var tour = TestTourFactory.CreateDraft(createdByUserId: owner, slug: "stable-slug");
        var sameSlug = tour.Slug;

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        repo.IsSlugReservedAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(tour, sameSlug), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Slug tag evicted exactly once (no duplicate eviction when slug is unchanged).
        await cache.Received(1).RemoveByTagAsync(
            ContentToursCacheKeys.TagForTourSlug(sameSlug),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotFound_DoesNotEvictAnyCache()
    {
        var (handler, repo, _, cache, currentUser) = BuildSubject();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((Tour?)null);

        var fakeTour = TestTourFactory.CreateDraft();
        var result = await handler.Handle(BuildCommand(fakeTour, "new"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Forbidden_DoesNotEvictAnyCache()
    {
        var (handler, repo, _, cache, currentUser) = BuildSubject();
        var owner  = Guid.NewGuid();
        var caller = Guid.NewGuid();

        var tour = TestTourFactory.CreateDraft(createdByUserId: owner, slug: "untouched-slug");
        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(BuildCommand(tour, "new-slug"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task Unauthorized_DoesNotEvictAnyCache()
    {
        var (handler, _, _, cache, currentUser) = BuildSubject();
        currentUser.UserId.Returns((Guid?)null);

        var fakeTour = TestTourFactory.CreateDraft();
        var result = await handler.Handle(BuildCommand(fakeTour, "new"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, default);
    }

    [Fact]
    public async Task RowVersionConflict_DoesNotEvictAnyCache()
    {
        var (handler, repo, _, cache, currentUser) = BuildSubject();
        var owner = Guid.NewGuid();

        var tour = TestTourFactory.CreateDraft(createdByUserId: owner, slug: "untouched-slug");
        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        repo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var staleVersion = new byte[] { 0xFF, 0x00 };
        var result = await handler.Handle(
            BuildCommand(tour, "new-slug", rowVersion: staleVersion),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync((string)default!, default);
    }
}
