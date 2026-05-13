using ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Tests.Unit;

public sealed class RemoveBusinessStaffCommandHandlerTests
{
    private static (
        RemoveBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessStaffCommandHandler>>();

        var handler = new RemoveBusinessStaffCommandHandler(
            staffRepo, uow, outbox, currentUser, cache, logger);

        return (handler, staffRepo, uow, currentUser);
    }

    private static StaffEntity SeedActiveStaffForBusiness(Guid ownerId)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var staff = StaffEntity.Create(business.Id, Guid.NewGuid(), BusinessStaffRole.Manager);

        // Wire navigation
        typeof(StaffEntity)
            .GetProperty(nameof(StaffEntity.Business))!
            .SetValue(staff, business);

        return staff;
    }

    [Fact]
    public async Task ReturnsUnauthorizedWhenNotAuthenticated()
    {
        var (handler, _, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Unauthorized");
    }

    [Fact]
    public async Task ReturnsNotFoundWhenStaffMissing()
    {
        var (handler, staffRepo, _, currentUser) = BuildSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((StaffEntity?)null);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.BusinessStaff.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenStandardUserIsNotOwner()
    {
        var (handler, staffRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Auth.Forbidden");
    }

    [Fact]
    public async Task ReturnsConflictWhenStaffAlreadyInactive()
    {
        var (handler, staffRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);
        staff.Deactivate();

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "BusinessStaff.AlreadyInactive.Conflict");
    }

    [Fact]
    public async Task SucceedsWhenCallerIsOwner()
    {
        var (handler, staffRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(ownerId);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        staff.IsActive.Should().BeFalse();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    public async Task SucceedsWhenCallerHasAdminTierRole(string role)
    {
        var (handler, staffRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId);

        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(callerId);
        currentUser.Roles.Returns(new[] { role });
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(staff);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(staff.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        staff.IsActive.Should().BeFalse();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
