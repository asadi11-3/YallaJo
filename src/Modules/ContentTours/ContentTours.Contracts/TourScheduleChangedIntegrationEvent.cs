using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

/// <summary>
/// Raised when a TourSchedule row is created, updated, or deleted.
/// Consumers: Booking (invalidate availability cache), Analytics (tour-activity tracking).
/// </summary>
public sealed record TourScheduleChangedIntegrationEvent(
    Guid ScheduleId,
    Guid TourId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    TourEntityChangeType ChangeType
) : IntegrationEventBase;
