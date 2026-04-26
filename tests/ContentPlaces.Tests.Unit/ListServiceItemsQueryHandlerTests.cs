using System.Linq.Expressions;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using ServiceItemEntity = ContentPlaces.Domain.Entities.ServiceItem;

namespace ContentPlaces.Tests.Unit;

public sealed class ListServiceItemsQueryHandlerTests
{
    private static (
        ListServiceItemsQueryHandler Handler,
        IServiceItemRepository ItemRepo,
        IBusinessRepository BusinessRepo,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var itemRepo = Substitute.For<IServiceItemRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<ListServiceItemsQueryHandler>>();

        var handler = new ListServiceItemsQueryHandler(
            itemRepo, businessRepo, currentUser, logger);

        return (handler, itemRepo, businessRepo, currentUser);
    }

    /// <summary>
    /// Captures the filter expression passed to <see cref="IServiceItemRepository.SelectAsync"/>
    /// so the test can verify whether the filter restricts to IsAvailable=true.
    /// </summary>
    private static Expression<Func<ServiceItemEntity, bool>>? CapturedFilter(
        IServiceItemRepository repo)
    {
        var calls = repo.ReceivedCalls();
        foreach (var call in calls)
        {
            if (call.GetMethodInfo().Name != nameof(IServiceItemRepository.SelectAsync))
                continue;

            // SelectAsync(selector, filter, orderBy, asNoTracking, ct) — filter is arg index 1.
            var args = call.GetArguments();
            return args[1] as Expression<Func<ServiceItemEntity, bool>>;
        }
        return null;
    }

    /// <summary>
    /// Tests whether the captured filter selects an IsAvailable=false service item.
    /// Returns true if elevated (no IsAvailable check); false if non-elevated (filtered out).
    /// </summary>
    private static bool FilterAllowsUnavailableItem(
        Expression<Func<ServiceItemEntity, bool>>? filter,
        Guid businessId)
    {
        filter.Should().NotBeNull();
        var compiled = filter!.Compile();
        var unavailable = TestServiceItemFactory.CreateActiveServiceItem(businessId);
        unavailable.SetAvailability(false);
        return compiled(unavailable);
    }

    [Fact]
    public async Task AnonymousCallerSeesOnlyAvailableItems()
    {
        var (handler, itemRepo, businessRepo, currentUser) = BuildSubject();
        var businessId = Guid.NewGuid();
        currentUser.IsAuthenticated.Returns(false);
        currentUser.UserId.Returns((Guid?)null);
        currentUser.Roles.Returns(Array.Empty<string>());
        businessRepo
            .GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns((Business?)null);
        itemRepo
            .SelectAsync(
                Arg.Any<Expression<Func<ServiceItemEntity, ServiceItemDto>>>(),
                Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(),
                Arg.Any<Func<IQueryable<ServiceItemEntity>, IOrderedQueryable<ServiceItemEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ServiceItemDto>());

        var result = await handler.Handle(
            new ListServiceItemsQuery(businessId, IsElevated: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        FilterAllowsUnavailableItem(CapturedFilter(itemRepo), businessId).Should().BeFalse();
    }

    [Fact]
    public async Task StandardNonOwnerSeesOnlyAvailableItems()
    {
        var (handler, itemRepo, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .SelectAsync(
                Arg.Any<Expression<Func<ServiceItemEntity, ServiceItemDto>>>(),
                Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(),
                Arg.Any<Func<IQueryable<ServiceItemEntity>, IOrderedQueryable<ServiceItemEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ServiceItemDto>());

        var result = await handler.Handle(
            new ListServiceItemsQuery(business.Id, IsElevated: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        FilterAllowsUnavailableItem(CapturedFilter(itemRepo), business.Id).Should().BeFalse();
    }

    [Fact]
    public async Task OwnerSeesAllItemsIncludingUnavailable()
    {
        var (handler, itemRepo, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .SelectAsync(
                Arg.Any<Expression<Func<ServiceItemEntity, ServiceItemDto>>>(),
                Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(),
                Arg.Any<Func<IQueryable<ServiceItemEntity>, IOrderedQueryable<ServiceItemEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ServiceItemDto>());

        var result = await handler.Handle(
            new ListServiceItemsQuery(business.Id, IsElevated: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Handler re-evaluates ownership locally; owner gets elevated view regardless of request flag.
        FilterAllowsUnavailableItem(CapturedFilter(itemRepo), business.Id).Should().BeTrue();
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task AdminTierSeesAllItemsIncludingUnavailable(string role)
    {
        var (handler, itemRepo, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        businessRepo
            .GetByIdAsync(business.Id, Arg.Any<CancellationToken>())
            .Returns(business);
        itemRepo
            .SelectAsync(
                Arg.Any<Expression<Func<ServiceItemEntity, ServiceItemDto>>>(),
                Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(),
                Arg.Any<Func<IQueryable<ServiceItemEntity>, IOrderedQueryable<ServiceItemEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ServiceItemDto>());

        var result = await handler.Handle(
            new ListServiceItemsQuery(business.Id, IsElevated: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        FilterAllowsUnavailableItem(CapturedFilter(itemRepo), business.Id).Should().BeTrue();
    }

    [Fact]
    public async Task RequestIsElevatedTrueShortCircuitsFilter()
    {
        // If the endpoint sets IsElevated=true (e.g., admin caller), the handler must honour it
        // even if local re-evaluation would have come up false (e.g., business not found).
        var (handler, itemRepo, businessRepo, currentUser) = BuildSubject();
        var businessId = Guid.NewGuid();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(businessId, Arg.Any<CancellationToken>())
            .Returns((Business?)null);
        itemRepo
            .SelectAsync(
                Arg.Any<Expression<Func<ServiceItemEntity, ServiceItemDto>>>(),
                Arg.Any<Expression<Func<ServiceItemEntity, bool>>>(),
                Arg.Any<Func<IQueryable<ServiceItemEntity>, IOrderedQueryable<ServiceItemEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ServiceItemDto>());

        var result = await handler.Handle(
            new ListServiceItemsQuery(businessId, IsElevated: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        FilterAllowsUnavailableItem(CapturedFilter(itemRepo), businessId).Should().BeTrue();
    }
}
