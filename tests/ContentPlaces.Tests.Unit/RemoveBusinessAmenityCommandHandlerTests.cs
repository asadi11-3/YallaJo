using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
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
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessAmenityCommandHandler>>();

        var handler = new RemoveBusinessAmenityCommandHandler(
            amenityRepo, uow, currentUser, cache, logger);

        return (handler, amenityRepo, uow, currentUser);
    }

    private static BusinessAmenity SeedAmenityForBusiness(Guid ownerId)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var amenity = BusinessAmenity.Create(business.Id, "WiFi", null, 0);

        // Wire navigation manually (private setter via reflection-free pattern: cast to dynamic
        // does not work for private setters; instead use the EF-friendly Business property setter
        // through the entity's own surface. Since BusinessAmenity.Business is { get; private set; },
        // we set it via reflection.)
        typeof(BusinessAmenity)
            .GetProperty(nameof(BusinessAmenity.Business))!
            .SetValue(amenity, business);

        return amenity;
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenAmenityMissing()
    {
        var (handler, amenityRepo, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
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
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, amenityRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        amenityRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(amenity);

        var result = await handler.Handle(
            new RemoveBusinessAmenityCommand(amenity.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, amenityRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
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

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, amenityRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var amenity = SeedAmenityForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
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
