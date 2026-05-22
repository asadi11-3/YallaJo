using System.Linq.Expressions;
using ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;
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

public sealed class AddBusinessAmenityCommandHandlerTests
{
    private static (
        AddBusinessAmenityCommandHandler Handler,
        IBusinessAmenityRepository AmenityRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var amenityRepo = Substitute.For<IBusinessAmenityRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessAmenityCommandHandler>>();

        var handler = new AddBusinessAmenityCommandHandler(
            amenityRepo, businessRepo, uow, cache, logger);

        return (handler, amenityRepo, businessRepo, uow, currentUser);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessMissing()
    {
        var (handler, _, businessRepo, _, _) = BuildSubject();
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(Guid.NewGuid(), Guid.NewGuid(), "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Business.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, _, businessRepo, _, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(business.Id, callerId, "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Business.Forbidden");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, amenityRepo, businessRepo, uow, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        amenityRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessAmenity, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(business.Id, ownerId, "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await amenityRepo.Received(1).AddAsync(Arg.Any<BusinessAmenity>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsDuplicateWhenAmenityExists()
    {
        var (handler, amenityRepo, businessRepo, _, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        amenityRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessAmenity, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(
            new AddBusinessAmenityCommand(business.Id, ownerId, "WiFi", null, 0),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "BusinessAmenity.Duplicate");
    }
}
