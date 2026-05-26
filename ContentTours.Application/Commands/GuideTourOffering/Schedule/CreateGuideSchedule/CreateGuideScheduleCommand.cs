using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.CreateGuideSchedule;

public sealed record CreateGuideScheduleCommand(
    Guid TourId,
    Guid TourGuideId,
    byte DayOfWeek,
    string StartTime,
    string? EndTime) : ICommand<Guid>;
