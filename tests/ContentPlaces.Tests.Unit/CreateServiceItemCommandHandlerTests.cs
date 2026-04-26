using System.Linq.Expressions;
using ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;
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
using YallaJo.SharedKernel.Domain.Event;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Tests.Unit;

public sealed class CreateServiceItemCommandHandlerTests
{
    private static (
        CreateServiceItemCommandHandler Handler,
        IServiceItemRepository ItemRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        IContentPlacesOutboxWriter Outbox,
        ICurrentUser CurrentUser,
        HybridCache Cache) BuildSubject()
    {
        var itemRepo = Substitute.For<IServiceItemRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<CreateServiceItemCommandHandler>>();

        var handler = new CreateServiceItemCommandHandler(
            itemRepo, businessRepo, uow, outbox, currentUser, cache, logger);

        return (handler, itemRepo, businessRepo, uow, outbox, currentUser, cache);
    }

    private static CreateServiceItemCommand BuildCommand(Guid businessId)
        => new(
            BusinessId: businessId,
            Name: "Guided Tour",
            Price: 50m,
            DurationMinutes: 90,
            MaxCapacity: 12,
            Currency: "JOD",
            Category: ServiceCategory.Activity,
            Description: "Test",
            SortOrder: 0);

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, _, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);

        var result = await handler.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessMissing()
    {
        var (handler, _, businessRepo, _, _, currentUser, _) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Business.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, _, businessRepo, _, _, currentUser, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(BuildCommand(business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task ReturnsConflictWhenDuplicateName()
    {
        var (handler, itemRepo, businessRepo, _, _, currentUser, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(BuildCommand(business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "ServiceItem.Duplicate");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwnerAndEnqueuesOutboxOnce()
    {
        var (handler, itemRepo, businessRepo, uow, outbox, currentUser, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await itemRepo.Received(1).AddAsync(Arg.Any<ServiceItemEntity>(), Arg.Any<CancellationToken>());
        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, itemRepo, businessRepo, _, outbox, currentUser, _) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .AnyAsync(Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(BuildCommand(business.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await itemRepo.Received(1).AddAsync(Arg.Any<ServiceItemEntity>(), Arg.Any<CancellationToken>());
        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());
    }
}
