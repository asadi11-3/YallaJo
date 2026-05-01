using System.Linq.Expressions;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;
using ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;
using ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Behaviour tests for the three schedule command handlers (Create / Update / Delete).
/// Drives each handler through NSubstitute mocks of <see cref="ITourScheduleRepository"/>,
/// <see cref="ITourRepository"/>, <see cref="IContentToursOutboxWriter"/>,
/// <see cref="IContentToursUnitOfWork"/>, <see cref="IScheduleBookingCountService"/>,
/// <see cref="ICurrentUser"/> — same style as <c>DeleteTourCommandHandlerTests</c>.
/// </summary>
public sealed class TourScheduleCommandHandlerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly TimeOnly Start = new(9, 0);
    private static readonly TimeOnly End = new(11, 0);

    // ── helpers ───────────────────────────────────────────────────────────────

    private static (
        CreateTourScheduleCommandHandler Handler,
        ITourRepository TourRepo,
        ITourScheduleRepository ScheduleRepo,
        IContentToursUnitOfWork Uow,
        IContentToursOutboxWriter Outbox,
        ICurrentUser CurrentUser) BuildCreateSubject()
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
        return (handler, tourRepo, scheduleRepo, uow, outbox, currentUser);
    }

    private static (
        UpdateTourScheduleCommandHandler Handler,
        ITourRepository TourRepo,
        ITourScheduleRepository ScheduleRepo,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildUpdateSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var scheduleRepo = Substitute.For<ITourScheduleRepository>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<UpdateTourScheduleCommandHandler>>();

        var handler = new UpdateTourScheduleCommandHandler(
            tourRepo, scheduleRepo, uow, outbox, cache, currentUser, logger);
        return (handler, tourRepo, scheduleRepo, uow, currentUser);
    }

    private static (
        DeleteTourScheduleCommandHandler Handler,
        ITourRepository TourRepo,
        ITourScheduleRepository ScheduleRepo,
        IScheduleBookingCountService BookingCountService,
        IContentToursUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildDeleteSubject()
    {
        var tourRepo = Substitute.For<ITourRepository>();
        var scheduleRepo = Substitute.For<ITourScheduleRepository>();
        var bookingCount = Substitute.For<IScheduleBookingCountService>();
        var uow = Substitute.For<IContentToursUnitOfWork>();
        var outbox = Substitute.For<IContentToursOutboxWriter>();
        var cache = Substitute.For<HybridCache>();
        var currentUser = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<DeleteTourScheduleCommandHandler>>();

        var handler = new DeleteTourScheduleCommandHandler(
            tourRepo, scheduleRepo, bookingCount, uow, outbox, cache, currentUser, logger);
        return (handler, tourRepo, scheduleRepo, bookingCount, uow, currentUser);
    }

    private static void StubGetAll(ITourScheduleRepository repo, params TourSchedule[] rows)
        => repo.GetAllAsync(
                filter:       Arg.Any<Expression<Func<TourSchedule, bool>>>(),
                include:      Arg.Any<Func<IQueryable<TourSchedule>, IQueryable<TourSchedule>>?>(),
                orderBy:      Arg.Any<Func<IQueryable<TourSchedule>, IOrderedQueryable<TourSchedule>>?>(),
                asNoTracking: Arg.Any<bool>(),
                ct:           Arg.Any<CancellationToken>())
            .Returns(rows.ToList());

    // ── Create — recurrence patterns ──────────────────────────────────────────

    [Fact]
    public async Task Create_OncePattern_StagesOneScheduleAndOutboxEvent()
    {
        var (handler, tourRepo, scheduleRepo, _, outbox, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo); // no existing rows

        var date = Today.AddDays(2);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null,
            CustomDates: new[] { date },
            StartTime:   Start,
            EndTime:     End,
            ValidFrom:   null,
            ValidTo:     null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().Be(1);
        result.Value!.Skipped.Should().Be(0);

        await scheduleRepo.Received(1).AddAsync(
            Arg.Is<TourSchedule>(s => s.TourId == tour.Id && s.DayOfWeek == (byte)date.DayOfWeek),
            Arg.Any<CancellationToken>());
        outbox.Received(1).Enqueue(Arg.Is<IIntegrationEvent>(e => e is TourScheduleChangedIntegrationEvent));
    }

    [Fact]
    public async Task Create_DailyPatternAcrossWeek_CollapsesIntoOneRowPerWeeklySlot()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        // 7 days → 7 candidates → 7 unique weekly slots (one per weekday) → Created = 7, Skipped = 0
        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Daily,
            DaysOfWeek:  null, CustomDates: null,
            StartTime:   Start, EndTime: End,
            ValidFrom:   Today, ValidTo: Today.AddDays(6),
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().Be(7);
        result.Value!.Skipped.Should().Be(0);

        // 14 days → 14 candidates → still only 7 unique weekly slots → Created 7, Skipped 7.
        var (handler2, tourRepo2, scheduleRepo2, _, _, currentUser2) = BuildCreateSubject();
        currentUser2.UserId.Returns(owner);
        tourRepo2.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo2);

        var result2 = await handler2.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Daily,
            DaysOfWeek:  null, CustomDates: null,
            StartTime:   Start, EndTime: End,
            ValidFrom:   Today, ValidTo: Today.AddDays(13),
            IsActive:    true), CancellationToken.None);

        result2.IsSuccess.Should().BeTrue();
        result2.Value!.Created.Should().Be(7);
        result2.Value!.Skipped.Should().Be(7);
    }

    [Fact]
    public async Task Create_WeeklyPattern_OnlyEmitsRequestedWeekdays()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        var addedDays = new List<byte>();
        scheduleRepo
            .When(r => r.AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>()))
            .Do(call => addedDays.Add(call.Arg<TourSchedule>().DayOfWeek));

        var monWedFri = new byte[]
        {
            (byte)DayOfWeek.Monday,
            (byte)DayOfWeek.Wednesday,
            (byte)DayOfWeek.Friday,
        };

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Weekly,
            DaysOfWeek:  monWedFri, CustomDates: null,
            StartTime:   Start, EndTime: End,
            ValidFrom:   Today, ValidTo: Today.AddDays(20),
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        addedDays.Should().OnlyContain(d => monWedFri.Contains(d));
        addedDays.Distinct().Order().Should().Equal(monWedFri.Order());
    }

    [Fact]
    public async Task Create_CustomDateOutOfRange_Returns422CustomDateOutOfRange()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Custom,
            DaysOfWeek:  null,
            CustomDates: new[] { Today.AddDays(120) },
            StartTime:   Start, EndTime: End,
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.CustomDateOutOfRange");
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_OverHardLimit_Returns422ExpansionTooLarge_AndNothingPersisted()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        var dates = Enumerable.Range(0, 121).Select(i => Today.AddDays(i % 60)).ToList();

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Custom,
            DaysOfWeek:  null, CustomDates: dates,
            StartTime:   Start, EndTime: End,
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.ExpansionTooLarge");
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ValidFromBeyondCap_Returns400PatternParamsInvalid()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Daily,
            DaysOfWeek:  null, CustomDates: null,
            StartTime:   Start, EndTime: End,
            ValidFrom:   Today.AddDays(100), ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.PatternParamsInvalid");
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_OverlapWithExisting_Returns422OverlapDetected()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        // Existing Monday 09:00–12:00 schedule blocks a new Monday 10:00–11:00 candidate.
        StubGetAll(scheduleRepo, TestScheduleFactory.Create(
            tour.Id, dayOfWeek: (byte)DayOfWeek.Monday,
            startTime: new TimeOnly(9, 0), endTime: new TimeOnly(12, 0)));

        var nextMonday = Today;
        while (nextMonday.DayOfWeek != DayOfWeek.Monday) nextMonday = nextMonday.AddDays(1);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null,
            CustomDates: new[] { nextMonday },
            StartTime:   new TimeOnly(10, 0),
            EndTime:     new TimeOnly(11, 0),
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.OverlapDetected");
    }

    [Fact]
    public async Task Create_IdempotentSkip_OnExistingDayStartTime()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        // Existing exact match.
        StubGetAll(scheduleRepo, TestScheduleFactory.Create(
            tour.Id, dayOfWeek: (byte)Today.DayOfWeek,
            startTime: Start, endTime: End));

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null, CustomDates: new[] { Today },
            StartTime:   Start, EndTime: End,
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().Be(0);
        result.Value!.Skipped.Should().Be(1);
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_IdempotentSkip_EvenWhenEndTimeDiffers()
    {
        // PDF chosen design: skip on (TourId, DayOfWeek, StartTime); EndTime drift is logged
        // at Warning but does not change the outcome.
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo, TestScheduleFactory.Create(
            tour.Id, dayOfWeek: (byte)Today.DayOfWeek,
            startTime: Start, endTime: new TimeOnly(13, 0))); // existing 9:00–13:00

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null, CustomDates: new[] { Today },
            StartTime:   Start, EndTime: new TimeOnly(11, 0),  // request 9:00–11:00
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().Be(0);
        result.Value!.Skipped.Should().Be(1);
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, scheduleRepo, _, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null, CustomDates: null,
            StartTime:   Start, EndTime: End,
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
        await scheduleRepo.DidNotReceive().AddAsync(Arg.Any<TourSchedule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ConcurrencyConflict_Returns409TourScheduleConcurrencyConflict()
    {
        var (handler, tourRepo, scheduleRepo, uow, _, currentUser) = BuildCreateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        StubGetAll(scheduleRepo);
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new DbUpdateConcurrencyException());

        var result = await handler.Handle(new CreateTourScheduleCommand(
            TourId:      tour.Id,
            Pattern:     TourSchedulePattern.Once,
            DaysOfWeek:  null, CustomDates: new[] { Today },
            StartTime:   Start, EndTime: End,
            ValidFrom:   null, ValidTo: null,
            IsActive:    true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.ConcurrencyConflict");
    }

    // ── Update — overlap + ownership ──────────────────────────────────────────

    [Fact]
    public async Task Update_NonOwnerNonAdmin_Returns403TourNotOwner()
    {
        var (handler, tourRepo, scheduleRepo, _, currentUser) = BuildUpdateSubject();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);

        currentUser.UserId.Returns(attacker);
        currentUser.Roles.Returns(new[] { AppRoles.User });
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);

        var result = await handler.Handle(new UpdateTourScheduleCommand(
            TourId:     tour.Id,
            ScheduleId: Guid.NewGuid(),
            DayOfWeek:  1, StartTime: Start, EndTime: End, IsActive: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "Tour.NotOwner");
    }

    [Fact]
    public async Task Update_OverlapWithSiblingSchedule_Returns422OverlapDetected()
    {
        var (handler, tourRepo, scheduleRepo, _, currentUser) = BuildUpdateSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);
        var schedule = TestScheduleFactory.Create(tour.Id, dayOfWeek: 1,
            startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0));
        var sibling = TestScheduleFactory.Create(tour.Id, dayOfWeek: 1,
            startTime: new TimeOnly(11, 0), endTime: new TimeOnly(13, 0));

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        scheduleRepo.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(schedule);
        StubGetAll(scheduleRepo, sibling);

        // Update target to overlap sibling
        var result = await handler.Handle(new UpdateTourScheduleCommand(
            TourId:     tour.Id,
            ScheduleId: schedule.Id,
            DayOfWeek:  1, StartTime: new TimeOnly(10, 0), EndTime: new TimeOnly(12, 0),
            IsActive:   true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.OverlapDetected");
    }

    // ── Delete — guard + concurrency ──────────────────────────────────────────

    [Fact]
    public async Task Delete_FutureBookingsExist_Returns409DeleteBlocked()
    {
        var (handler, tourRepo, scheduleRepo, bookingCount, _, currentUser) = BuildDeleteSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);
        var schedule = TestScheduleFactory.Create(tour.Id);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        scheduleRepo.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(schedule);
        bookingCount.GetFutureBookingCountForScheduleAsync(schedule.Id, Arg.Any<CancellationToken>())
            .Returns(3);

        var result = await handler.Handle(
            new DeleteTourScheduleCommand(tour.Id, schedule.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.DeleteBlocked");
        scheduleRepo.DidNotReceive().Remove(Arg.Any<TourSchedule>());
    }

    [Fact]
    public async Task Delete_NoFutureBookings_Succeeds()
    {
        var (handler, tourRepo, scheduleRepo, bookingCount, uow, currentUser) = BuildDeleteSubject();
        var owner = Guid.NewGuid();
        var tour = TestTourFactory.CreateDraft(createdByUserId: owner);
        var schedule = TestScheduleFactory.Create(tour.Id);

        currentUser.UserId.Returns(owner);
        tourRepo.GetByIdAsync(tour.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(tour);
        scheduleRepo.GetByIdAsync(schedule.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(schedule);
        bookingCount.GetFutureBookingCountForScheduleAsync(schedule.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await handler.Handle(
            new DeleteTourScheduleCommand(tour.Id, schedule.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        scheduleRepo.Received(1).Remove(schedule);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
