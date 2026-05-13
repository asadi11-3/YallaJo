using ContentPlaces.Application.Commands.Place.VerifyPlace;
using ContentPlaces.Application.Interfaces;
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

public sealed class VerifyPlaceCommandHandlerTests
{
    private static (
        VerifyPlaceCommandHandler Handler,
        IPlaceRepository PlaceRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildSubject()
    {
        var placeRepo = Substitute.For<IPlaceRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<VerifyPlaceCommandHandler>>();

        var handler = new VerifyPlaceCommandHandler(
            placeRepo, uow, currentUser, cache, logger);

        return (handler, placeRepo, uow, currentUser, cache);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, uow, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(
            new VerifyPlaceCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotAdminTier()
    {
        var (handler, placeRepo, uow, currentUser, _) = BuildSubject();
        var creatorId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(creatorId);
        currentUser.Roles.Returns(new[] { AppRoles.User });

        var result = await handler.Handle(
            new VerifyPlaceCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");

        await placeRepo.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceMissingForAdminTierCaller()
    {
        var (handler, placeRepo, uow, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((PlaceEntity?)null);

        var result = await handler.Handle(
            new VerifyPlaceCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(x => x.Code == "Place.NotFound");
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
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

        var result = await handler.Handle(
            new VerifyPlaceCommand(place.Id, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        place.IsVerified.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync($"place:{place.Id}", Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync("places", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnverifySetsIsVerifiedFalse()
    {
        var (handler, placeRepo, uow, currentUser, _) = BuildSubject();
        var creatorId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(creatorId);
        place.SetVerified(true);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.Admin });
        placeRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(place);

        var result = await handler.Handle(
            new VerifyPlaceCommand(place.Id, false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        place.IsVerified.Should().BeFalse();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
