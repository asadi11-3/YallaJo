using ContentPlaces.Application.Commands.Place.VerifyPlace;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using PlaceEntity = ContentPlaces.Domain.Entities.Place;

namespace ContentPlaces.Tests.Unit;

public sealed class VerifyPlaceCommandHandlerTests
{
    private static (
        VerifyPlaceCommandHandler Handler,
        IPlaceRepository PlaceRepo,
        IContentPlacesUnitOfWork Uow,
        HybridCache Cache) BuildSubject()
    {
        var placeRepo = Substitute.For<IPlaceRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<VerifyPlaceCommandHandler>>();

        var handler = new VerifyPlaceCommandHandler(
            placeRepo, uow, cache, logger);

        return (handler, placeRepo, uow, cache);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenPlaceMissing()
    {
        var (handler, placeRepo, uow, _) = BuildSubject();
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

    [Fact]
    public async Task SucceedsAndSetsIsVerifiedTrue()
    {
        var (handler, placeRepo, uow, cache) = BuildSubject();
        var creatorId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(creatorId);

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
        var (handler, placeRepo, uow, _) = BuildSubject();
        var creatorId = Guid.NewGuid();
        var place = TestPlaceFactory.CreatePlace(creatorId);
        place.SetVerified(true);

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
