using ContentTours.Domain.Entities;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Helpers for building <see cref="TourSchedule"/> rows in tests via the public factory.
/// Mirrors the style of <see cref="TestTourFactory"/>.
/// </summary>
internal static class TestScheduleFactory
{
    public static TourSchedule Create(
        Guid tourId,
        byte dayOfWeek = 1,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        bool isActive = true)
        => TourSchedule.Create(
            tourId:    tourId,
            dayOfWeek: dayOfWeek,
            startTime: startTime ?? new TimeOnly(9, 0),
            endTime:   endTime,
            isActive:  isActive);
}
