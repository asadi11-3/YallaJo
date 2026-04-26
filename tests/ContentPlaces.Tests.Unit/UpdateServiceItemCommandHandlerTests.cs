using System.Linq.Expressions;
using ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Tests.Unit;

public sealed class UpdateServiceItemCommandHandlerTests
{
    private static (
        UpdateServiceItemCommandHandler Handler,
        IServiceItemRepository ItemRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var itemRepo = Substitute.For<IServiceItemRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<UpdateServiceItemCommandHandler>>();

        var handler = new UpdateServiceItemCommandHandler(
            itemRepo, businessRepo, uow, currentUser, cache, logger);

        return (handler, itemRepo, businessRepo, uow, currentUser);
    }

    private static UpdateServiceItemCommand BuildCommand(Guid itemId, Guid businessId)
        => new(
            Id: itemId,
            BusinessId: businessId,
            Name: "Updated Service",
            Price: 75m,
            DurationMinutes: 120,
            MaxCapacity: 8,
            Currency: "JOD",
            Category: ServiceCategory.Activity,
            Description: "Updated",
            SortOrder: 1);

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            BuildCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenItemMissing()
    {
        var (handler, itemRepo, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ServiceItemEntity?)null);

        var result = await handler.Handle(
            BuildCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "ServiceItem.NotFound");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessIdMismatch()
    {
        var (handler, itemRepo, _, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var item = TestServiceItemFactory.CreateActiveServiceItem(Guid.NewGuid());

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(item);

        var mismatchedBusiness = Guid.NewGuid();
        var result = await handler.Handle(
            BuildCommand(item.Id, mismatchedBusiness), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "ServiceItem.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, itemRepo, businessRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var item = TestServiceItemFactory.CreateActiveServiceItem(business.Id);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(item);
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(
            BuildCommand(item.Id, business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task ReturnsConflictWhenDuplicateName()
    {
        var (handler, itemRepo, businessRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var item = TestServiceItemFactory.CreateActiveServiceItem(business.Id);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(item);
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(
            BuildCommand(item.Id, business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "ServiceItem.Duplicate");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, itemRepo, businessRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var item = TestServiceItemFactory.CreateActiveServiceItem(business.Id);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(item);
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            BuildCommand(item.Id, business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, itemRepo, businessRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var item = TestServiceItemFactory.CreateActiveServiceItem(business.Id);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(item);
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            BuildCommand(item.Id, business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
