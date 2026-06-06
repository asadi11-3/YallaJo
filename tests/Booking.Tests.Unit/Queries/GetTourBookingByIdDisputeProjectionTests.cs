using Booking.Application.Interfaces;
using Booking.Application.Queries.GetTourBookingById;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booking.Tests.Unit.Queries;

/// <summary>
/// FE-1A: verifies <see cref="GetTourBookingByIdQueryHandler"/> projects the dispute
/// lifecycle (DisputedAt / Reason and, once resolved, ResolvedAt / ResolutionNotes)
/// into <see cref="TourBookingDetailDto.Dispute"/> so the booking detail page can
/// render the dispute panels.
/// </summary>
public sealed class GetTourBookingByIdDisputeProjectionTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TourId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProviderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SlotId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid AdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static BookingPricing DefaultPricing() =>
        new(
            Subtotal: 90m,
            DiscountAmount: 0m,
            LoyaltyAmount: 0m,
            TotalAmount: 90m,
            CommissionRate: 0.10m,
            CommissionAmount: 9m,
            Currency: "JOD",
            LineItems: new List<BookingLineItem> { new(TierType.Adult, 2, 45m, "JOD") });

    private static TourBooking CreateCompletedBooking()
    {
        var booking = TourBooking.Create(
            userId: UserId,
            tourId: TourId,
            providerId: ProviderId,
            guideId: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            availabilitySlotId: SlotId,
            participantCount: 2,
            pricing: DefaultPricing(),
            reference: BookingReference.Compose(new DateOnly(2026, 7, 1), "A7X3K9"),
            refundPolicySnapshot: "{}",
            isInstantBooking: true,
            paymentExpiresAt: DateTime.UtcNow.AddMinutes(10),
            lineItemsJson: "[]");

        booking.Confirm(ConfirmationSource.PaymentWebhook);
        booking.Complete(ProviderId);
        return booking;
    }

    private static GetTourBookingByIdQueryHandler CreateHandler(TourBooking booking)
    {
        var repo = Substitute.For<ITourBookingRepository>();
        repo.GetByIdWithDetailsAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        return new GetTourBookingByIdQueryHandler(
            repo,
            Substitute.For<IBookingTourSnapshotReader>(),
            Substitute.For<IBookingProviderSnapshotReader>(),
            NullLogger<GetTourBookingByIdQueryHandler>.Instance);
    }

    [Fact]
    public async Task Completed_booking_without_dispute_has_null_Dispute_block()
    {
        var booking = CreateCompletedBooking();
        var handler = CreateHandler(booking);

        var result = await handler.Handle(
            new GetTourBookingByIdQuery(booking.Id, UserId, ViewerIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Dispute.Should().BeNull();
    }

    [Fact]
    public async Task Disputed_booking_projects_DisputedAt_and_Reason()
    {
        var booking = CreateCompletedBooking();
        booking.OpenDispute(UserId, "The guide never showed up for the tour.");
        var handler = CreateHandler(booking);

        var result = await handler.Handle(
            new GetTourBookingByIdQuery(booking.Id, UserId, ViewerIsAdmin: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BookingStatus.Disputed);
        result.Value.Dispute.Should().NotBeNull();
        result.Value.Dispute!.Reason.Should().Be("The guide never showed up for the tour.");
        result.Value.Dispute.ResolvedAt.Should().BeNull();
        result.Value.Dispute.ResolutionNotes.Should().BeNull();
    }

    [Fact]
    public async Task Resolved_booking_projects_ResolutionNotes_and_ResolvedAt()
    {
        var booking = CreateCompletedBooking();
        booking.OpenDispute(UserId, "The guide never showed up for the tour.");
        booking.ResolveDispute(AdminId, "Refunded in full as a goodwill gesture.");
        var handler = CreateHandler(booking);

        var result = await handler.Handle(
            new GetTourBookingByIdQuery(booking.Id, AdminId, ViewerIsAdmin: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BookingStatus.Resolved);
        result.Value.Dispute.Should().NotBeNull();
        result.Value.Dispute!.ResolutionNotes.Should().Be("Refunded in full as a goodwill gesture.");
        result.Value.Dispute.ResolvedAt.Should().NotBeNull();
    }
}
