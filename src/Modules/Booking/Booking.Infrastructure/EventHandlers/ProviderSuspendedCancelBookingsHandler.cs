using Accounts.Contracts.IntegrationEvents;
using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using Booking.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Cancels all active tour bookings for a provider when that provider is suspended.
///
/// <para>
/// The Booking module has no InboxStore, so this handler is not idempotent at the
/// message-ID level. Duplicate delivery is mitigated by the domain guard in
/// <c>TourBooking.Cancel</c> — already-cancelled bookings throw and are skipped.
/// </para>
///
/// <para>
/// Active states considered cancellable:
/// <see cref="BookingStatus.AwaitingPayment"/>,
/// <see cref="BookingStatus.PendingConfirmation"/>,
/// <see cref="BookingStatus.Confirmed"/>.
/// </para>
/// </summary>
public sealed class ProviderSuspendedCancelBookingsHandler(
    BookingDbContext dbContext,
    IBookingUnitOfWork unitOfWork,
    ILogger<ProviderSuspendedCancelBookingsHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderSuspendedIntegrationEvent>>
{
    private const int BatchSize = 100;

    public async Task Handle(
        IntegrationEventNotification<ProviderSuspendedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var cancellationCtx = new BookingCancellationContext(
            Source:              CancellationSource.Admin,
            Reason:              $"Provider account suspended: {evt.Reason}",
            ProviderInitiated:   false,
            ForceMajeureOverride: true);   // 100% refund for provider-side suspensions

        var activeStatuses = new[]
        {
            BookingStatus.AwaitingPayment,
            BookingStatus.PendingConfirmation,
            BookingStatus.Confirmed
        };

        var totalCancelled = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // ProviderId on TourBooking is the provider's application UserId.
            var batch = await dbContext.TourBookings
                .Where(b => b.ProviderId == evt.UserId && activeStatuses.Contains(b.Status))
                .OrderBy(b => b.Id)
                .Take(BatchSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var booking in batch)
            {
                try
                {
                    booking.Cancel(cancellationCtx, refundPercentage: 100m);
                }
                catch (InvalidOperationException ex)
                {
                    // Domain guard: booking is already in a terminal state.
                    logger.LogWarning(
                        "Booking: skipping cancel for Booking {BookingId} (already terminal): {Message}",
                        booking.Id, ex.Message);
                }
            }

            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            totalCancelled += batch.Count;

            logger.LogDebug(
                "Booking: cancelled batch of {Count} bookings for provider UserId={UserId}.",
                batch.Count, evt.UserId);

            if (batch.Count < BatchSize)
            {
                break;
            }
        }

        logger.LogInformation(
            "Booking: Cancelled {Total} bookings for ProviderSuspended UserId={UserId} ApplicationId={ApplicationId}.",
            totalCancelled, evt.UserId, evt.ApplicationId);
    }
}
