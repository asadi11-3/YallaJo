using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

/// <summary>
/// Published by BookingReminderService when a confirmed booking's slot is starting soon.
/// Consumed by Messaging module to send tourist + guide notifications.
/// </summary>
public sealed record BookingReminderIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    Guid AvailabilitySlotId,
    DateTime ReminderSentAt) : IntegrationEventBase;
