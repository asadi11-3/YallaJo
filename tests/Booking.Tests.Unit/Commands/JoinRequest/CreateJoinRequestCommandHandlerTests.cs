using Booking.Application.Commands.JoinRequest.CreateJoinRequest;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands.JoinRequest;

public sealed class CreateJoinRequestCommandHandlerTests
{
    private static readonly Guid BookingOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RequesterId   = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TourId        = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProviderId    = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid SlotId        = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static (CreateJoinRequestCommandHandler handler,
        IJoinRequestRepository jrRepo,
        ITourBookingRepository bookingRepo,
        IAvailabilitySlotRepository slotRepo,
        IBookingUnitOfWork uow,
        ICurrentUser user) BuildSut()
    {
        var jrRepo = Substitute.For<IJoinRequestRepository>();
        var bookingRepo = Substitute.For<ITourBookingRepository>();
        var slotRepo = Substitute.For<IAvailabilitySlotRepository>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var user = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<CreateJoinRequestCommandHandler>>();
        var handler = new CreateJoinRequestCommandHandler(jrRepo, bookingRepo, slotRepo, uow, user, logger);
        return (handler, jrRepo, bookingRepo, slotRepo, uow, user);
    }

    private static TourBooking MakeBooking(Guid ownerUserId, BookingStatus status = BookingStatus.Confirmed)
    {
        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 1, 90m, "JOD") });
        var reference = BookingReference.Compose(new DateOnly(2026, 7, 1), "JRAAAA");
        var booking = TourBooking.Create(
            userId: ownerUserId,
            tourId: TourId,
            providerId: ProviderId,
            availabilitySlotId: SlotId,
            participantCount: 1,
            pricing: pricing,
            reference: reference,
            refundPolicySnapshot: "{}",
            isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");

        if (status == BookingStatus.Confirmed)
        {
            booking.Confirm(ConfirmationSource.PaymentWebhook);
        }
        return booking;
    }

    private static AvailabilitySlot MakeSlot(int max = 10, int booked = 0, int locked = 0)
    {
        var slot = AvailabilitySlot.CreateForTour(
            tourGuideId: Guid.NewGuid(),
            tourId: TourId,
            date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            start: new TimeOnly(9, 0),
            end: new TimeOnly(11, 0),
            maxCapacity: max);
        if (booked > 0) slot.Book(booked);
        if (locked > 0) slot.Lock(locked);
        return slot;
    }

    private static void AuthAs(ICurrentUser user, Guid id)
    {
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(id);
    }

    [Fact]
    public async Task Unauthenticated_returns_Unauthorized()
    {
        var (handler, _, _, _, uow, user) = BuildSut();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var result = await handler.Handle(new CreateJoinRequestCommand(Guid.NewGuid(), 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Booking_not_found_returns_NotFound()
    {
        var (handler, _, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, RequesterId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((TourBooking?)null);

        var result = await handler.Handle(new CreateJoinRequestCommand(Guid.NewGuid(), 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.BookingNotFound");
    }

    [Fact]
    public async Task Booking_not_confirmed_returns_Invalid()
    {
        var (handler, _, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, RequesterId);
        var booking = MakeBooking(BookingOwnerId, status: BookingStatus.AwaitingPayment);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.BookingNotConfirmed");
    }

    [Fact]
    public async Task Self_join_blocked_even_for_owner()
    {
        var (handler, _, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId); // requester == booking owner
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.SelfJoin");
    }

    [Fact]
    public async Task Self_join_blocked_even_for_admin()
    {
        var (handler, _, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        user.HasPermission(Arg.Any<string>()).Returns(true); // admin
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.SelfJoin");
    }

    [Fact]
    public async Task Duplicate_pending_returns_Conflict()
    {
        var (handler, jrRepo, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, RequesterId);
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        jrRepo.ExistsPendingForUserAndBookingAsync(booking.Id, RequesterId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.Duplicate");
    }

    [Fact]
    public async Task Capacity_full_returns_Conflict()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, _, user) = BuildSut();
        AuthAs(user, RequesterId);
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        jrRepo.ExistsPendingForUserAndBookingAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        // 10 max, 10 booked => 0 available
        var slot = MakeSlot(max: 10, booked: 10);
        slotRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(slot);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.CapacityFull");
    }

    [Fact]
    public async Task Slot_missing_returns_NotFound()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, _, user) = BuildSut();
        AuthAs(user, RequesterId);
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        jrRepo.ExistsPendingForUserAndBookingAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        slotRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns((AvailabilitySlot?)null);

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 1, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.SlotNotFound");
    }

    [Fact]
    public async Task Happy_path_returns_Created_and_adds_pending_request()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, uow, user) = BuildSut();
        AuthAs(user, RequesterId);
        var booking = MakeBooking(BookingOwnerId);
        bookingRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        jrRepo.ExistsPendingForUserAndBookingAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        slotRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(MakeSlot(10));

        var result = await handler.Handle(new CreateJoinRequestCommand(booking.Id, 2, "please?"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.TourBookingId.Should().Be(booking.Id);
        result.Value.UserId.Should().Be(RequesterId);
        result.Value.ParticipantCount.Should().Be(2);
        result.Value.Status.Should().Be(JoinRequestStatus.Pending);
        await jrRepo.Received(1).AddAsync(Arg.Any<Booking.Domain.Entities.JoinRequest>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
