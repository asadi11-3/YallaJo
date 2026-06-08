using System.Linq.Expressions;
using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
using ContentPlaces.Application.Commands.BusinessStaff.RemoveBusinessStaff;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Contracts.BusinessStaff;
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
using YallaJo.SharedKernel.Domain.Event;
using StaffEntity = ContentPlaces.Domain.Entities.BusinessStaff;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// Regression tests for P0-004: BusinessStaff Add/Remove handlers enqueue integration
/// events directly via <see cref="IContentPlacesOutboxWriter"/> because BusinessStaff
/// is not <c>IAggregateRoot</c> and its raised domain events would otherwise be
/// silently dropped by the UnitOfWork dispatcher.
/// </summary>
public sealed class BusinessStaffOutboxPublishingTests
{
    // ── AddBusinessStaff ─────────────────────────────────────────────────────

    private static (
        AddBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        IContentPlacesOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildAddSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid()); // default; tests override below
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessStaffCommandHandler>>();

        var handler = new AddBusinessStaffCommandHandler(
            staffRepo, businessRepo, uow, outbox, cache, currentUser, logger);

        return (handler, staffRepo, businessRepo, uow, outbox, currentUser);
    }

    [Fact]
    public async Task AddBusinessStaff_OnSuccess_EnqueuesExactlyOneAddedEventWithCorrectPayload()
    {
        var (handler, staffRepo, businessRepo, uow, outbox, currentUser) = BuildAddSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var newStaffUserId = Guid.NewGuid();
        const BusinessStaffRole role = BusinessStaffRole.Manager;
        currentUser.UserId.Returns(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(Arg.Any<Expression<Func<StaffEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Capture the staff entity at the point AddAsync is called (before SaveChanges).
        StaffEntity? capturedStaff = null;
        await staffRepo.AddAsync(
            Arg.Do<StaffEntity>(s => capturedStaff = s),
            Arg.Any<CancellationToken>());

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, newStaffUserId, role),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedStaff.Should().NotBeNull();

        // Exactly one BusinessStaffAddedIntegrationEvent enqueued with matching payload.
        outbox.Received(1).Enqueue(Arg.Is<BusinessStaffAddedIntegrationEvent>(e =>
            e.BusinessId == business.Id
            && e.UserId == newStaffUserId
            && e.StaffId == capturedStaff!.Id
            && e.Role == role.ToString()));

        // No other integration event types were enqueued.
        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());

        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBusinessStaff_OnNotFound_DoesNotEnqueueOrSave()
    {
        var (handler, _, businessRepo, uow, outbox, _) = BuildAddSubject();
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(Guid.NewGuid(), Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        outbox.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBusinessStaff_OnForbidden_DoesNotEnqueueOrSave()
    {
        var (handler, _, businessRepo, uow, outbox, currentUser) = BuildAddSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        currentUser.UserId.Returns(callerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        outbox.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── RemoveBusinessStaff ──────────────────────────────────────────────────

    private static (
        RemoveBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IContentPlacesUnitOfWork Uow,
        IContentPlacesOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildRemoveSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<RemoveBusinessStaffCommandHandler>>();

        var handler = new RemoveBusinessStaffCommandHandler(
            staffRepo, uow, outbox, currentUser, cache, logger);

        return (handler, staffRepo, uow, outbox, currentUser);
    }

    private static StaffEntity SeedActiveStaffForBusiness(Guid ownerId, Guid? userId = null)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        var staff = StaffEntity.Create(
            business.Id,
            userId ?? Guid.NewGuid(),
            BusinessStaffRole.Manager);

        // Wire navigation
        typeof(StaffEntity)
            .GetProperty(nameof(StaffEntity.Business))!
            .SetValue(staff, business);

        return staff;
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnSuccess_EnqueuesExactlyOneRemovedEventWithCorrectPayload()
    {
        var (handler, staffRepo, uow, outbox, currentUser) = BuildRemoveSubject();
        var ownerId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var staff = SeedActiveStaffForBusiness(ownerId, staffUserId);

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

        outbox.Received(1).Enqueue(Arg.Is<BusinessStaffRemovedIntegrationEvent>(e =>
            e.StaffId == staff.Id
            && e.BusinessId == staff.BusinessId
            && e.UserId == staffUserId));

        outbox.Received(1).Enqueue(Arg.Any<IIntegrationEvent>());

        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnUnauthorized_DoesNotEnqueueOrSave()
    {
        var (handler, _, uow, outbox, currentUser) = BuildRemoveSubject();
        currentUser.IsAuthenticated.Returns(false);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        outbox.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnNotFound_DoesNotEnqueueOrSave()
    {
        var (handler, staffRepo, uow, outbox, currentUser) = BuildRemoveSubject();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(Guid.NewGuid());
        staffRepo
            .GetByIdWithBusinessAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((StaffEntity?)null);

        var result = await handler.Handle(
            new RemoveBusinessStaffCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        outbox.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveBusinessStaff_OnForbidden_DoesNotEnqueueOrSave()
    {
        var (handler, staffRepo, uow, outbox, currentUser) = BuildRemoveSubject();
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
        outbox.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
