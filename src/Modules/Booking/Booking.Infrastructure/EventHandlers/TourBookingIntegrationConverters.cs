using Booking.Contracts.IntegrationEvents;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Infrastructure.EventHandlers;

/// <summary>
/// Converts <see cref="TourBookingCreatedDomainEvent"/> into the outbound integration event.
/// Dispatched BEFORE SaveChanges inside the same UoW so the OutboxMessage row is persisted atomically.
/// </summary>
internal sealed class PublishTourBookingCreatedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingCreatedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingCreatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingCreatedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            AvailabilitySlotId: e.AvailabilitySlotId,
            ParticipantCount: e.ParticipantCount,
            TotalAmount: e.TotalAmount,
            Currency: e.Currency,
            Reference: e.Reference,
            IsInstantBooking: e.IsInstantBooking,
            BookedAt: e.OccurredOn);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.created.v1 for booking {BookingId}",
            e.BookingId);
    }
}

/// <summary>Converts <see cref="TourBookingConfirmedDomainEvent"/> to outbound integration event.</summary>
internal sealed class PublishTourBookingConfirmedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingConfirmedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingConfirmedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingConfirmedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingConfirmedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            ConfirmedAt: e.ConfirmedAt,
            ConfirmationSource: e.Source.ToString());

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.confirmed.v1 for booking {BookingId} via {Source}",
            e.BookingId,
            e.Source);
    }
}

/// <summary>Converts <see cref="TourBookingRejectedDomainEvent"/> to outbound integration event.</summary>
internal sealed class PublishTourBookingRejectedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingRejectedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingRejectedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingRejectedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingRejectedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            AvailabilitySlotId: e.AvailabilitySlotId,
            ParticipantCount: e.ParticipantCount,
            RejectedAt: e.RejectedAt,
            Reason: e.Reason,
            RefundAmount: e.RefundAmount,
            Currency: e.Currency);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.rejected.v1 for booking {BookingId} (refund {Refund} {Currency})",
            e.BookingId,
            e.RefundAmount,
            e.Currency);
    }
}

/// <summary>Converts <see cref="TourBookingCancelledDomainEvent"/> to outbound integration event.</summary>
internal sealed class PublishTourBookingCancelledHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingCancelledHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingCancelledDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingCancelledDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingCancelledIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            AvailabilitySlotId: e.AvailabilitySlotId,
            ParticipantCount: e.ParticipantCount,
            CancelledAt: e.CancelledAt,
            Source: e.Source.ToString(),
            Reason: e.Reason,
            RefundAmount: e.RefundAmount,
            Currency: e.Currency);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.cancelled.v1 for booking {BookingId} (source={Source}, refund={Refund} {Currency})",
            e.BookingId,
            e.Source,
            e.RefundAmount,
            e.Currency);
    }
}

/// <summary>Converts <see cref="TourBookingCompletedDomainEvent"/> to outbound integration event.</summary>
internal sealed class PublishTourBookingCompletedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingCompletedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingCompletedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingCompletedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingCompletedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            CompletedAt: e.CompletedAt,
            CompletedByUserId: e.CompletedByUserId);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.completed.v1 for booking {BookingId}",
            e.BookingId);
    }
}

/// <summary>Converts <see cref="TourBookingPaymentExpiredDomainEvent"/> to outbound integration event.</summary>
internal sealed class PublishTourBookingPaymentExpiredHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingPaymentExpiredHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingPaymentExpiredDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingPaymentExpiredDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingPaymentExpiredIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            AvailabilitySlotId: e.AvailabilitySlotId,
            ParticipantCount: e.ParticipantCount,
            CancelledAt: e.CancelledAt);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.payment-expired.v1 for booking {BookingId}",
            e.BookingId);
    }
}

/// <summary>
/// Phase-3 (G4a): Converts <see cref="TourBookingDisputedDomainEvent"/> to outbound integration event.
/// Booking owner has opened a dispute on a Completed booking within the 48h window.
/// Consumers: Messaging (notify provider + admin), Finance (correlate to payment dispute if any), Analytics.
/// </summary>
internal sealed class PublishTourBookingDisputedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingDisputedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingDisputedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingDisputedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingDisputedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            DisputedAt: e.DisputedAt,
            Reason: e.Reason);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.disputed.v1 for booking {BookingId}",
            e.BookingId);
    }
}

/// <summary>
/// Phase-3 (G4a): Converts <see cref="TourBookingDisputeResolvedDomainEvent"/> to outbound integration event.
/// Admin has resolved a Disputed booking. Consumers: Messaging (notify owner + provider), Analytics.
/// </summary>
internal sealed class PublishTourBookingDisputeResolvedHandler(IBookingOutboxWriter outbox, ILogger<PublishTourBookingDisputeResolvedHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingDisputeResolvedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<TourBookingDisputeResolvedDomainEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.Event;
        var integration = new TourBookingDisputeResolvedIntegrationEvent(
            BookingId: e.BookingId,
            UserId: e.UserId,
            TourId: e.TourId,
            ProviderId: e.ProviderId,
            ResolvedByAdminId: e.ResolvedByAdminId,
            ResolvedAt: e.ResolvedAt,
            ResolutionNotes: e.ResolutionNotes);

        await outbox.WriteAsync(integration, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Enqueued booking.tour-booking.dispute-resolved.v1 for booking {BookingId} (by admin {AdminId})",
            e.BookingId,
            e.ResolvedByAdminId);
    }
}
