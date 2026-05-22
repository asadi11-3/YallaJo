using Booking.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.EventHandlers;

/// <summary>
/// Consumes <c>booking.tour-booking.completed.v1</c> to update the
/// <see cref="BookingEligibilitySnapshot"/>, enabling verified-booking badge (S-R1).
/// </summary>
internal sealed class BookingTourBookingCompletedHandler(
    IBookingEligibilitySnapshotRepository snapshotRepository,
    ISocialInboxStore inboxStore,
    ISocialUnitOfWork unitOfWork,
    ILogger<BookingTourBookingCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourBookingCompletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourBookingCompletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug("Booking completed event {MessageId} already processed (idempotent).",
                notification.MessageId);
            return;
        }

        var evt = notification.Event;

        // Only Tour target type for eligibility snapshots
        var existing = await snapshotRepository.GetAsync(
            evt.UserId, ReviewTargetType.Tour, evt.TourId, ct);

        if (existing is null)
        {
            var snapshot = new BookingEligibilitySnapshot(
                evt.UserId, ReviewTargetType.Tour, evt.TourId, evt.CompletedAt);
            await snapshotRepository.UpsertAsync(snapshot, ct);
        }
        else
        {
            existing.RecordBooking(evt.CompletedAt);
            await snapshotRepository.UpsertAsync(existing, ct);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Booking eligibility snapshot updated for user {UserId}, tour {TourId} (completedAt={CompletedAt})",
            evt.UserId, evt.TourId, evt.CompletedAt);
    }
}
