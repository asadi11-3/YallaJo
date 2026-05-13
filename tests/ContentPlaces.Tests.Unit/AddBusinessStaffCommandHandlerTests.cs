using System.Linq.Expressions;
using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
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
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Tests.Unit;

public sealed class AddBusinessStaffCommandHandlerTests
{
    private static (
        AddBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessStaffCommandHandler>>();

        var handler = new AddBusinessStaffCommandHandler(
            staffRepo, businessRepo, uow, outbox, currentUser, cache, logger);

        return (handler, staffRepo, businessRepo, uow, currentUser);
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(Guid.NewGuid(), Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessMissing()
    {
        var (handler, _, businessRepo, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(Guid.NewGuid(), Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.Business.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, _, businessRepo, _, currentUser) = BuildSubject();
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
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task ReturnsConflictWhenStaffAlreadyActive()
    {
        var (handler, staffRepo, businessRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessStaff, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "BusinessStaff.Duplicate.Conflict");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, staffRepo, businessRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessStaff, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Manager),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await staffRepo.Received(1).AddAsync(Arg.Any<BusinessStaff>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, staffRepo, businessRepo, uow, currentUser) = BuildSubject();
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
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessStaff, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
    }
}
