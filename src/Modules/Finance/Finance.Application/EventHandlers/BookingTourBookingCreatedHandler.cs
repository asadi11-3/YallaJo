using Booking.Contracts.IntegrationEvents;
using Finance.Application.Interfaces;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Application.EventHandlers;

/// <summary>
/// Consumes <see cref="TourBookingCreatedIntegrationEvent"/> from the Booking module and
/// materialises a Finance-local <see cref="PaymentExpectation"/> snapshot.
/// Provides a deterministic anchor for POST /payments/initiate without round-tripping to Booking.
/// </summary>
public sealed class BookingTourBookingCreatedHandler(
    IPaymentExpectationRepository paymentExpectations,
    IFinanceInboxStore inboxStore,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<BookingTourBookingCreatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourBookingCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Idempotency guard — skip if this outbox message was already processed.
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Finance: TourBookingCreated message {MessageId} (BookingId={BookingId}) already processed — skipping.",
                notification.MessageId, notification.Event.BookingId);
            return;
        }

        var evt = notification.Event;

        // Defensive: skip if a snapshot already exists for this booking (handler retry).
        if (await paymentExpectations.ExistsForBookingAsync(evt.BookingId, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Finance: PaymentExpectation already exists for booking {BookingId} — marking inbox and skipping.",
                evt.BookingId);

            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var expectation = PaymentExpectation.Create(
            bookingId: evt.BookingId,
            userId: evt.UserId,
            providerId: evt.ProviderId,
            tourId: evt.TourId,
            expectedAmount: new Money(evt.TotalAmount, evt.Currency),
            reference: evt.Reference,
            timeProvider: timeProvider);

        await paymentExpectations.AddAsync(expectation, ct).ConfigureAwait(false);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Finance: PaymentExpectation {ExpectationId} seeded for booking {BookingId} ({Amount} {Currency}).",
            expectation.Id, evt.BookingId, evt.TotalAmount, evt.Currency);
    }
}
