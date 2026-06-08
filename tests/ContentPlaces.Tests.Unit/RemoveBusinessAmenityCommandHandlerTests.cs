using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Tests.Unit;

public sealed class RemoveBusinessAmenityCommandHandlerTests
{
    private static (
        RemoveBusinessAmenityCommandHandler Handler,
        IBusinessAmenityRepository AmenityRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var amenityRepo = Substitute.For<IBusinessAmenityRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid()); // default; tests override below
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessAmenityCommandHandler>>();

        var handler = new RemoveBusinessAmenityCommandHandler(
            amenityRepo, uow, cache, currentUser, logger);

        return (handler, amenityRepo, uow, currentUser);
    }

    private static BusinessAmenity SeedAmenityForBusiness(Guid ownerId)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var amenity = BusinessAmenity.Create(business.Id, "WiFi", null, 0);

        typeof(BusinessAmenity)
            .GetProperty(nameof(BusinessAmenity.Business))!
            .SetValue(amenity, business);

        return amenity;
    }

    [Fact]
    public async Task ReturnsNotFoundWhenAmenityMissing()
    {
        var (handler, amenityRepo, _, _) = BuildSubject();
        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((BusinessAmenity?)null);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.BusinessAmenity.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenActingUserIsNotOwner()
    {
        var (handler, amenityRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);
        currentUser.UserId.Returns(callerId);

        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(amenity);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(amenity.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Business.Forbidden");
    }

    [Fact]
    public async Task SucceedsWhenActingUserIsOwner()
    {
        var (handler, amenityRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);
        currentUser.UserId.Returns(ownerId);

        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(amenity);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(amenity.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        amenityRepo.Received(1).Remove(amenity);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
