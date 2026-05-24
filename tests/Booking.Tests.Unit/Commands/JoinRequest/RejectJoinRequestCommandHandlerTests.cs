using Booking.Application.Commands.JoinRequest.RejectJoinRequest;
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

public sealed class RejectJoinRequestCommandHandlerTests
{
    private static readonly Guid BookingOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RequesterId   = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherUserId   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TourId        = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ProviderId    = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SlotId        = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static readonly string AdminPermission =
        $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Update}";

    private static (RejectJoinRequestCommandHandler handler,
        IJoinRequestRepository jrRepo,
        ITourBookingRepository bookingRepo,
        IBookingUnitOfWork uow,
        ICurrentUser user) BuildSut()
    {
        var jrRepo = Substitute.For<IJoinRequestRepository>();
        var bookingRepo = Substitute.For<ITourBookingRepository>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var user = Substitute.For<ICurrentUser>();
        var logger = Substitute.For<ILogger<RejectJoinRequestCommandHandler>>();
        var handler = new RejectJoinRequestCommandHandler(jrRepo, bookingRepo, uow, user, logger);
        return (handler, jrRepo, bookingRepo, uow, user);
    }

    private static TourBooking MakeConfirmedBooking()
    {
        var pricing = new BookingPricing(
            Subtotal: 90m, DiscountAmount: 0m, LoyaltyAmount: 0m, TotalAmount: 90m,
            CommissionRate: 0.10m, CommissionAmount: 9m, Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 1, 90m, "JOD") });
        var booking = TourBooking.Create(
            userId: BookingOwnerId, tourId: TourId, providerId: ProviderId,
            availabilitySlotId: SlotId, participantCount: 1, pricing: pricing,
            reference: BookingReference.Compose(new DateOnly(2026, 7, 1), "JRAAAC"),
            refundPolicySnapshot: "{}", isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10), lineItemsJson: "[]");
        booking.Confirm(ConfirmationSource.PaymentWebhook);
        return booking;
    }

    private static Booking.Domain.Entities.JoinRequest MakePending(Guid bookingId)
        => Booking.Domain.Entities.JoinRequest.Create(bookingId, RequesterId, 1, null);

    private static void AuthAs(ICurrentUser user, Guid id, bool admin = false)
    {
        user.IsAuthenticated.Returns(true);
        user.UserId.Returns(id);
        user.HasPermission(AdminPermission).Returns(admin);
    }

    [Fact]
    public async Task Unauthenticated_returns_Unauthorized()
    {
        var (handler, _, _, uow, user) = BuildSut();
        user.IsAuthenticated.Returns(false);
        user.UserId.Returns((Guid?)null);

        var result = await handler.Handle(new RejectJoinRequestCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinRequest_not_found_returns_NotFound()
    {
        var (handler, jrRepo, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        jrRepo.GetByIdTrackedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Booking.Domain.Entities.JoinRequest?)null);

        var result = await handler.Handle(new RejectJoinRequestCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Non_pending_returns_Invalid()
    {
        var (handler, jrRepo, _, _, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var jr = MakePending(Guid.NewGuid());
        jr.Approve();
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);

        var result = await handler.Handle(new RejectJoinRequestCommand(jr.Id, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.NotPending");
    }

    [Fact]
    public async Task Non_owner_non_admin_returns_Forbidden()
    {
        var (handler, jrRepo, bookingRepo, _, user) = BuildSut();
        AuthAs(user, OtherUserId);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new RejectJoinRequestCommand(jr.Id, "no"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors.Should().ContainSingle(e => e.Code == "JoinRequest.OwnerMismatch");
    }

    [Fact]
    public async Task Owner_can_reject_with_reason()
    {
        var (handler, jrRepo, bookingRepo, uow, user) = BuildSut();
        AuthAs(user, BookingOwnerId);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new RejectJoinRequestCommand(jr.Id, "  group already chosen  "), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jr.Status.Should().Be(JoinRequestStatus.Rejected);
        jr.ResponseMessage.Should().Be("group already chosen");
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Admin_can_reject_even_if_not_owner()
    {
        var (handler, jrRepo, bookingRepo, uow, user) = BuildSut();
        AuthAs(user, OtherUserId, admin: true);
        var booking = MakeConfirmedBooking();
        var jr = MakePending(booking.Id);
        jrRepo.GetByIdTrackedAsync(jr.Id, Arg.Any<CancellationToken>()).Returns(jr);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(booking);

        var result = await handler.Handle(new RejectJoinRequestCommand(jr.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jr.Status.Should().Be(JoinRequestStatus.Rejected);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
