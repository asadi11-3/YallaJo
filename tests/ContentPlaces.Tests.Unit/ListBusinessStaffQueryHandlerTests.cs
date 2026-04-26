using System.Linq.Expressions;
using ContentPlaces.Application.Queries.BusinessStaff.Common;
using ContentPlaces.Application.Queries.BusinessStaff.ListBusinessStaff;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Tests.Unit;

public sealed class ListBusinessStaffQueryHandlerTests
{
    private static (
        ListBusinessStaffQueryHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IBusinessRepository BusinessRepo,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<ListBusinessStaffQueryHandler>>();

        var handler = new ListBusinessStaffQueryHandler(
            staffRepo, businessRepo, currentUser, logger);

        return (handler, staffRepo, businessRepo, currentUser);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new ListBusinessStaffQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessMissing()
    {
        var (handler, _, businessRepo, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new ListBusinessStaffQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.Business.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, _, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(
            new ListBusinessStaffQuery(business.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, staffRepo, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .SelectAsync(
                Arg.Any<Expression<Func<StaffEntity, BusinessStaffDto>>>(),
                Arg.Any<Expression<Func<StaffEntity, bool>>>(),
                Arg.Any<Func<IQueryable<StaffEntity>, IOrderedQueryable<StaffEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BusinessStaffDto>());

        var result = await handler.Handle(
            new ListBusinessStaffQuery(business.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, staffRepo, businessRepo, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .SelectAsync(
                Arg.Any<Expression<Func<StaffEntity, BusinessStaffDto>>>(),
                Arg.Any<Expression<Func<StaffEntity, bool>>>(),
                Arg.Any<Func<IQueryable<StaffEntity>, IOrderedQueryable<StaffEntity>>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<BusinessStaffDto>());

        var result = await handler.Handle(
            new ListBusinessStaffQuery(business.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
