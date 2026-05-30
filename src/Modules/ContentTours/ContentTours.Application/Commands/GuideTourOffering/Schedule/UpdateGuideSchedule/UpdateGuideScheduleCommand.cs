using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.UpdateGuideSchedule;

public sealed record UpdateGuideScheduleCommand(
    Guid ScheduleId,
    byte DayOfWeek,
    string StartTime,
    string? EndTime) : ICommand;
