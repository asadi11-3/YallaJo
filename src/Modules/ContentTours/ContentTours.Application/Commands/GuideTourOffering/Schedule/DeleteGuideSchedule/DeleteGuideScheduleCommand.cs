using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.DeleteGuideSchedule;

public sealed record DeleteGuideScheduleCommand(Guid ScheduleId) : ICommand;
