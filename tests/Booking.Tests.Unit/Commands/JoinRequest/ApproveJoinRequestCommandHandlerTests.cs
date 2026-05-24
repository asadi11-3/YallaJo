using Booking.Application.Commands.JoinRequest.ApproveJoinRequest;
using Booking.Application.Interfaces;
using Booking.Contracts.Authorization;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Tests.Unit.Commands.JoinRequest;

public sealed class ApproveJoinRequestCommandHandlerTests
{
    private static readonly Guid BookingOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RequesterId   = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherUserId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TourId        = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ProviderId    = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SlotId        = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly string AdminPermission =
        $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}";

    private static (ApproveJoinRequestCommandHandler handler,
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
        var logger = Substitute.For<ILogger<ApproveJoinRequestCommandHandler>>();
        var handler = new ApproveJoinRequestCommandHandler(jrRepo, bookingRepo, slotRepo, uow, user, logger);
        return (handler, jrRepo, bookingRepo, slotRepo, uow, user);
    }

    private static TourBooking MakeConfirmedBooking()
    {
        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 1, 90m, "JOD") });
        var reference = BookingReference.Compose(new DateOnly(2026, 7, 1), "JRAAAB");
        var booking = TourBooking.Create(
            userId: BookingOwnerId, tourId: TourId, providerId: ProviderId,
            availabilitySlotId: SlotId, participantCount: 1, pricing: pricing,
            reference: reference, refundPolicySnapshot: "{}",
            isInstantBooking: true, paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");
        booking.Confirm(ConfirmationSource.PaymentWebhook);
        return booking;
    }

    private static AvailabilitySlot MakeSlot(int max = 10, int booked = 0)
    {
        var slot = AvailabilitySlot.CreateForTour(
            tourGuideId: Guid.NewGuid(), tourId: TourId,
            date: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            start: new TimeOnly(9, 0), end: new TimeOnly(11, 0),
            maxCapacity: max);
        if (booked > 0) slot.Book(booked);
        return slot;
    }

    private static Booking.Domain.Entities.JoinRequest MakePending(Guid bookingId, int participants = 1)
        => Booking.Domain.Entities.JoinRequest.Create(bookingId, RequesterId, participants, null);

    private static void AuthAs(ICurrentUser user, Guid id, bool admin = false)
    {
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(id);
        user.HasPermission(AdminPermission).Returns(admin);
    }

    [Fact]
    public async Task Unauthenticated_returns_Unauthorized()
    {
        var (handler, _, _, _, uow, user) = BuildSut();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var result = await handler.Handle(new ApproveJoinRequestCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinRequest_not_found_returns_NotFound()
    {
        var (handler, jrRepo, _, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        jrRepo.GetByIdTrackedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.JoinRequest?)null);

        var result = await handler.Handle(new ApproveJoinRequestCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.NotFound");
    }

    [Fact]
    public async Task Non_pending_returns_Invalid()
    {
        var (handler, jrRepo, _, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var jr = MakePending(Guid.NewGuid());
        jr.Reject(null); // becomes Rejected
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.NotPending");
    }

    [Fact]
    public async Task Non_owner_non_admin_returns_Forbidden()
    {
        var (handler, jrRepo, bookingRepo, _, _, user) = BuildSut();
        AuthAs(user, OtherUserId); // some random user
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.OwnerMismatch");
    }

    [Fact]
    public async Task Booking_owner_can_approve_and_books_slot_capacity()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, uow, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id, participants: 2);
        var slot = MakeSlot(max: 10);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        slotRepo.GetByIdWithLockAsync(booking.AvailabilitySlotId, Arg.Any<CancellationToken>()).Returns(slot);

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        jr.Status.Should().Be(JoinRequestStatus.Approved);
        slot.BookedCount.Should().Be(2); // join request capacity was reserved on the slot
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_override_can_approve_even_if_not_owner()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, uow, user) = BuildSut();
        AuthAs(user, OtherUserId, admin: true);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        slotRepo.GetByIdWithLockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(MakeSlot(10));

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jr.Status.Should().Be(JoinRequestStatus.Approved);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_does_not_mutate_booking_participant_count()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var booking = MakeConfirmedBooking();
        var originalParticipantCount = booking.ParticipantCount;
        var jr = MakePending(booking.Id, participants: 3);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        slotRepo.GetByIdWithLockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(MakeSlot(20));

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.ParticipantCount.Should().Be(originalParticipantCount); // unchanged
    }

    [Fact]
    public async Task Capacity_full_at_approve_returns_Conflict()
    {
        var (handler, jrRepo, bookingRepo, slotRepo, uow, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id, participants: 5);
        // Slot is fully booked: max 5, booked 5 => available 0.
        var fullSlot = MakeSlot(max: 5, booked: 5);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);
        slotRepo.GetByIdWithLockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(fullSlot);

        var result = await handler.Handle(new ApproveJoinRequestCommand(jr.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.CapacityFull");
        jr.Status.Should().Be(JoinRequestStatus.Pending); // unchanged
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
