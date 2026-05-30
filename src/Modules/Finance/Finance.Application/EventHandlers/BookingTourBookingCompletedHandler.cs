using Booking.Contracts.IntegrationEvents;
using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Finance.Application.EventHandlers;

/// <summary>
/// Inbox-guarded consumer of <c>booking.tour-booking.completed.v1</c>.
/// Stamps EscrowReleaseEligibleAt on the matching Payment so the PayoutBatchingService
/// can sweep it in a future weekly run (F-R4).
/// </summary>
public sealed class BookingTourBookingCompletedHandler(
    IPaymentRepository paymentRepository,
    IFinanceInboxStore inboxStore,
    IFinanceUnitOfWork unitOfWork,
    IOptions<EscrowOptions> escrowOptions,
    ILogger<BookingTourBookingCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCompletedIntegrationEvent>>
{
    private readonly TimeSpan _holdPeriod = TimeSpan.FromDays(Math.Max(1, escrowOptions.Value.HoldDays));

    public async Task Handle(
        IntegrationEventNotification<TourBookingCompletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var evt = notification.Event;

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            return;
        }

        var bookingPayments = await paymentRepository.GetByBookingIdAsync(evt.BookingId, ct);
        var paid = bookingPayments
            .Where(p => p.PaymentType == PaymentType.Booking && p.Status == PaymentStatus.Completed)
            .FirstOrDefault();

        if (paid is null)
        {
            logger.LogWarning(
                "BookingCompleted received for booking {BookingId} but no Completed booking-payment found; skipping escrow stamp.",
                evt.BookingId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // Re-fetch tracked
        var tracked = await paymentRepository.GetByIdAsync(paid.Id, ct);
        if (tracked is null)
        {
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        tracked.MarkEscrowEligible(evt.CompletedAt, _holdPeriod);

        logger.LogInformation(
            "Stamped EscrowReleaseEligibleAt on payment {PaymentId} for booking {BookingId} (CompletedAt={CompletedAt}, EligibleAt={EligibleAt}).",
            tracked.Id,
            evt.BookingId,
            evt.CompletedAt,
            tracked.EscrowReleaseEligibleAt);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Configuration knob: hold period before completed payments become eligible for payout.
/// </summary>
public sealed class EscrowOptions
{
    public const string SectionName = "Finance:Payout";
    public int HoldDays { get; set; } = 7;
}
