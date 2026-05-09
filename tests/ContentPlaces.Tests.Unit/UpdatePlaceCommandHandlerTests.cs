using System.Linq.Expressions;
using ContentPlaces.Application.Commands.Place.UpdatePlace;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Tests.Unit;

public sealed class UpdatePlaceCommandHandlerTests
{
    private static (
        UpdatePlaceCommandHandler Handler,
        IPlaceRepository PlaceRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildSubject()
    {
        var placeRepo = Substitute.For<IPlaceRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<UpdatePlaceCommandHandler>>();

        var handler = new UpdatePlaceCommandHandler(
            placeRepo, uow, currentUser, cache, logger);

        return (handler, placeRepo, uow, currentUser, cache);
    }

    private static UpdatePlaceCommand BuildCommand(Guid placeId, string slug = "updated-slug")
        => new(
            Id: placeId,
            Name: "Updated Place",
            Slug: slug,
            PlaceType: PlaceType.Attraction,
            Latitude: 32.0m,
            Longitude: 35.0m,
            Description: null,
            Address: null,
            City: null,
            Country: null,
            PostalCode: null,
            Phone: null,
            Email: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null);

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, uow, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenUserIdMissing()
    {
        var (handler, _, uow, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceMissing()
    {
        var (handler, placeRepo, uow, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((PlaceEntity?)null);

        var result = await handler.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(x => x.Code == "Place.NotFound");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, placeRepo, uow, currentUser, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(place);

        var result = await handler.Handle(BuildCommand(place.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsConflictWhenSlugDuplicate()
    {
        var (handler, placeRepo, uow, currentUser, cache) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(place);
        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(BuildCommand(place.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(x => x.Code == "Place.SlugConflict");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwnerAndInvalidatesCacheAfterSave()
    {
        var (handler, placeRepo, uow, currentUser, cache) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(place);
        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(place.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync($"place:{place.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync("places", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRoleEvenIfNotPlaceCreator(string role)
    {
        var (handler, placeRepo, uow, currentUser, cache) = BuildSubject();
        var creatorId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(creatorId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(place);
        placeRepo
            .AnyAsync(Arg.Any<Expression<Func<PlaceEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(place.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync($"place:{place.Id}", Arg.Any<CancellationToken>());
    }
}
