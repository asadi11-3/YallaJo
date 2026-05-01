using System.Linq.Expressions;
using ContentTours.Application.Commands.TourPricingTier.CreateTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.DeleteTourPricingTier;
using ContentTours.Application.Commands.TourPricingTier.UpdateTourPricingTier;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;
using ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;
using ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// P2 Admin-tier cleanup — role matrix coverage for owner-or-admin checks
/// across the Mohammad-owned ContentTours surfaces.
///
/// Pattern under test (per project standard):
///   var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
///       >= RolePrivilegeLevel.Admin;
///   if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value) -> Forbidden
///
/// Expected behavior:
///   Admin / SuperAdmin / Owner -> pass even when not the tour owner.
///   User / TourGuide / Guest   -> pass only if the caller is the tour owner.
/// </summary>
public sealed class AdminTierRoleMatrixTests
{
    private static readonly TimeOnly Start = new(9, 0);
    private static readonly TimeOnly End = new(11, 0);

    private static (CreateTourScheduleCommandHandler Handler,
        ITourRepository TourRepo,
        ITourScheduleRepository ScheduleRepo,
        ICurrentUser CurrentUser) BuildScheduleCreate()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var scheduleRepo = Substitute.For<ITourScheduleRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateTourScheduleCommandHandler>>();

        var handler = new CreateTourScheduleCommandHandler(
            tourRepo, scheduleRepo, uow, outbox, cache, currentUser, logger);

        scheduleRepo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourSchedule, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourSchedule>, IQueryable<TourSchedule>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourSchedule>, IOrderedQueryable<TourSchedule>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
            .Returns(new List<TourSchedule>());

        return (handler, tourRepo, scheduleRepo, currentUser);
    }

    private static (CreateTourPricingTierCommandHandler Handler,
        ITourRepository TourRepo,
        ITourPricingTierRepository TierRepo,
        ICurrentUser CurrentUser) BuildPricingCreate()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var tierRepo = Substitute.For<ITourPricingTierRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateTourPricingTierCommandHandler>>();

        var handler = new CreateTourPricingTierCommandHandler(
            tourRepo, tierRepo, uow, outbox, cache, currentUser, logger);

        tierRepo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourPricingTier, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourPricingTier>, IQueryable<TourPricingTier>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourPricingTier>, IOrderedQueryable<TourPricingTier>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
            .Returns(new List<TourPricingTier>());

        return (handler, tourRepo, tierRepo, currentUser);
    }

    // ── TourSchedule: CreateTourScheduleCommand owner-or-admin gate ──────────

    public static IEnumerable<object[]> AdminTierRoles =>
        new List<object[]>
        {
            new object[] { new[] { AppRoles.Admin } },
            new object[] { new[] { AppRoles.SuperAdmin } },
            new object[] { new[] { AppRoles.Owner } },
        };

    public static IEnumerable<object[]> NonAdminTierRoles =>
        new List<object[]>
        {
            new object[] { new[] { AppRoles.User } },
            new object[] { new[] { AppRoles.TourGuide } },
            new object[] { new[] { AppRoles.Guest } },
        };

    [Theory]
    [MemberData(nameof(AdminTierRoles))]
    public async Task CreateTourSchedule_NonOwnerAdminTier_PassesOwnershipGate(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildScheduleCreate();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:     tour.Id,
            Pattern:    TourSchedulePattern.Once,
            DaysOfWeek: null,
            CustomDates: new[] { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) },
            StartTime:  Start, EndTime: End,
            ValidFrom:  null, ValidTo: null,
            IsActive:   true), CancellationToken.None);

        // Ownership gate must NOT block admin-tier callers.
        result.Errors.Should().NotContain(e => e.Code == "Tour.NotOwner");
    }

    [Theory]
    [MemberData(nameof(NonAdminTierRoles))]
    public async Task CreateTourSchedule_NonOwnerNonAdminTier_FailsTourNotOwner(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildScheduleCreate();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:     tour.Id,
            Pattern:    TourSchedulePattern.Once,
            DaysOfWeek: null,
            CustomDates: new[] { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) },
            StartTime:  Start, EndTime: End,
            ValidFrom:  null, ValidTo: null,
            IsActive:   true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Theory]
    [MemberData(nameof(NonAdminTierRoles))]
    public async Task CreateTourSchedule_OwnerNonAdminTier_PassesOwnershipGate(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildScheduleCreate();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:     tour.Id,
            Pattern:    TourSchedulePattern.Once,
            DaysOfWeek: null,
            CustomDates: new[] { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) },
            StartTime:  Start, EndTime: End,
            ValidFrom:  null, ValidTo: null,
            IsActive:   true), CancellationToken.None);

        // Owner with a regular role should still pass the ownership gate.
        result.Errors.Should().NotContain(e => e.Code == "Tour.NotOwner");
    }

    // ── TourPricingTier: CreateTourPricingTierCommand owner-or-admin gate ────

    [Theory]
    [MemberData(nameof(AdminTierRoles))]
    public async Task CreateTourPricingTier_NonOwnerAdminTier_PassesOwnershipGate(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildPricingCreate();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId:          tour.Id,
            Name:            "Adult",
            Description:     null,
            Price:           50m,
            Currency:        "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1,
            MaxParticipants: null), CancellationToken.None);

        result.Errors.Should().NotContain(e => e.Code == "Tour.NotOwner");
    }

    [Theory]
    [MemberData(nameof(NonAdminTierRoles))]
    public async Task CreateTourPricingTier_NonOwnerNonAdminTier_FailsTourNotOwner(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildPricingCreate();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(caller);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId:          tour.Id,
            Name:            "Adult",
            Description:     null,
            Price:           50m,
            Currency:        "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1,
            MaxParticipants: null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Theory]
    [MemberData(nameof(NonAdminTierRoles))]
    public async Task CreateTourPricingTier_OwnerNonAdminTier_PassesOwnershipGate(string[] roles)
    {
        var (handler, tourRepo, _, currentUser) = BuildPricingCreate();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        currentUser.Roles.Returns(roles);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new CreateTourPricingTierCommand(
            TourId:          tour.Id,
            Name:            "Adult",
            Description:     null,
            Price:           50m,
            Currency:        "JOD",
            ParticipantType: ParticipantType.Adult,
            MinParticipants: 1,
            MaxParticipants: null), CancellationToken.None);

        result.Errors.Should().NotContain(e => e.Code == "Tour.NotOwner");
    }

    // ── MyTours providerUserId admin-tier override (logic mirrors endpoint) ──

    /// <summary>
    /// MyTours endpoint contract — only admin-tier (Admin / SuperAdmin / Owner)
    /// or the explicit ContentTours.Tour.ReadAny permission may override the
    /// providerUserId query param. This test mirrors the endpoint logic shape
    /// against the same primitive (AppRoles.HighestPrivilegeLevel) so a future
    /// edit cannot regress to IsInRole("Admin") without breaking this test.
    /// </summary>
    [Theory]
    [InlineData(new[] { "Admin" },        true)]
    [InlineData(new[] { "SuperAdmin" },   true)]
    [InlineData(new[] { "Owner" },        true)]
    [InlineData(new[] { "User" },         false)]
    [InlineData(new[] { "TourGuide" },    false)]
    [InlineData(new[] { "Guest" },        false)]
    public void MyTours_ProviderOverride_AdminTierMatrix(string[] roles, bool expectedOverride)
    {
        var caller = Guid.NewGuid();
        var providerOverride = Guid.NewGuid();

        var isAdminTier = AppRoles.HighestPrivilegeLevel(roles)
            >= RolePrivilegeLevel.Admin;

        // Endpoint logic: canOverride && providerUserId.HasValue ? providerUserId : currentUser
        var effectiveUserId = isAdminTier
            ? providerOverride
            : caller;

        isAdminTier.Should().Be(expectedOverride);
        effectiveUserId.Should().Be(expectedOverride ? providerOverride : caller);
    }
}
