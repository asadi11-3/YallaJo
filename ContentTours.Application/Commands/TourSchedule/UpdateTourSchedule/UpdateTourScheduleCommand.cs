using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;

public sealed record UpdateTourScheduleCommand(
    Guid TourId,
    Guid ScheduleId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive
) : ICommand;
