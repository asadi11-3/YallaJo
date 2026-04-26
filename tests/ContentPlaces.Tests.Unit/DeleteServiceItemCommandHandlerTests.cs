using ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Event;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Tests.Unit;

public sealed class DeleteServiceItemCommandHandlerTests
{
    private static (
        DeleteServiceItemCommandHandler Handler,
        IServiceItemRepository ItemRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        IContentPlacesOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var itemRepo = Substitute.For<IServiceItemRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<DeleteServiceItemCommandHandler>>();

        var handler = new DeleteServiceItemCommandHandler(
            itemRepo, businessRepo, uow, outbox, currentUser, cache, logger);

        return (handler, itemRepo, businessRepo, uow, outbox, currentUser);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new DeleteServiceItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenItemMissing()
    {
        var (handler, itemRepo, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        itemRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((ServiceItemEntity?)null);

        var result = await handler.Handle(
            new DeleteServiceItemCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "ServiceItem.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, itemRepo, businessRepo, _, _, currentUser) = BuildSubject();
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
            new DeleteServiceItemCommand(item.Id, Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwnerAndEnqueuesOutboxOnce()
    {
        var (handler, itemRepo, businessRepo, uow, outbox, currentUser) = BuildSubject();
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

        var result = await handler.Handle(
            new DeleteServiceItemCommand(item.Id, Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, itemRepo, businessRepo, uow, outbox, currentUser) = BuildSubject();
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

        var result = await handler.Handle(
            new DeleteServiceItemCommand(item.Id, Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
