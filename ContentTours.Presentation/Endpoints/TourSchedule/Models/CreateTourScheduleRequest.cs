using ContentTours.Application.Commands.TourSchedule.Common;

namespace ContentTours.Presentation.Endpoints.TourSchedule.Models;

public sealed record CreateTourScheduleRequest(
    TourSchedulePattern Pattern,
    List<byte>? DaysOfWeek,
    List<DateOnly>? CustomDates,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive = true);
